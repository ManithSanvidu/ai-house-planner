using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HousePlanner.API.Controllers;
using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HousePlanner.API.Tests.Controllers;

public class WorkflowControllerTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<WorkflowController>> _loggerMock;
    private readonly WorkflowController _controller;

    public WorkflowControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<WorkflowController>>();
        var clients = new Mock<IHttpClientFactory>();
        clients.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
        _controller = new WorkflowController(_dbContext, _loggerMock.Object, clients.Object,
            Mock.Of<IWorkflowService>(), Mock.Of<ICurrentUserContextService>());
    }

    [Fact]
    public async Task GetWorkflowStatus_ReturnsNotFound_WhenWorkflowDoesNotExist()
    {
        // Act
        var result = await _controller.GetWorkflowStatus(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetWorkflowStatus_ReturnsWorkflow_WhenNoDesignExists()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        _dbContext.WorkflowStates.Add(new WorkflowState
        {
            Id = workflowId,
            LandSubmissionId = Guid.NewGuid(),
            Status = "pending",
            TerrainType = "flat"
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetWorkflowStatus(workflowId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<WorkflowStatusResponseDto>(okResult.Value);
        
        Assert.Equal(workflowId, response.WorkflowId);
        Assert.Equal("pending", response.Status);
        Assert.Equal("flat", response.TerrainType);
        Assert.Null(response.Design);
    }

    [Fact]
    public async Task GetWorkflowStatus_ReturnsWorkflow_WithLatestDesign()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        
        var olderDesign = new HouseDesign
        {
            Id = Guid.NewGuid(),
            WorkflowStateId = workflowId,
            Version = 1,
            IsCurrent = false,
            FloorCount = 1,
            TotalBuiltUpAreaSqft = 1000m,
            FoundationType = "slab",
            LayoutJson = "{ \"rooms\": [] }"
        };

        var currentDesign = new HouseDesign
        {
            Id = Guid.NewGuid(),
            WorkflowStateId = workflowId,
            Version = 2,
            IsCurrent = true,
            FloorCount = 2,
            TotalBuiltUpAreaSqft = 1500m,
            FoundationType = "stepped",
            LayoutJson = "{ \"rooms\": [ { \"room_type\": \"living_room\", \"floor\": 1, \"x\": 0, \"y\": 0, \"width\": 10, \"length\": 10 } ] }",
            Rooms = new List<Room>
            {
                new Room { Id = Guid.NewGuid(), RoomType = "living_room", FloorNumber = 1, X = 0, Y = 0, Width = 10, Length = 10, AreaSqft = 100 }
            }
        };

        var workflow = new WorkflowState
        {
            Id = workflowId,
            LandSubmissionId = Guid.NewGuid(),
            Status = "design_generated",
            HouseDesigns = new List<HouseDesign> { olderDesign, currentDesign }
        };

        _dbContext.WorkflowStates.Add(workflow);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetWorkflowStatus(workflowId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<WorkflowStatusResponseDto>(okResult.Value);
        
        Assert.Equal(workflowId, response.WorkflowId);
        Assert.NotNull(response.Design);
        Assert.Equal(2, response.Design.Version);
        Assert.Equal(2, response.Design.FloorCount);
        Assert.Equal(1500m, response.Design.TotalBuiltUpAreaSqft);
        Assert.Single(response.Design.Rooms);
        Assert.Equal("living_room", response.Design.Rooms[0].RoomType);
    }

    [Fact]
    public async Task GetDesigns_ReturnsEverySavedVersion()
    {
        var workflowId = Guid.NewGuid();
        _dbContext.WorkflowStates.Add(new WorkflowState
        {
            Id = workflowId, LandSubmissionId = Guid.NewGuid(), Status = "design_generated",
            HouseDesigns = new List<HouseDesign>
            {
                Design(workflowId, 1, false), Design(workflowId, 2, true)
            }
        });
        await _dbContext.SaveChangesAsync();

        var result = Assert.IsType<OkObjectResult>(await _controller.GetDesigns(workflowId));
        var history = Assert.IsType<WorkflowDesignHistoryDto>(result.Value);

        Assert.Equal(2, history.Designs.Count);
        Assert.Equal(new[] { 2, 1 }, history.Designs.Select(d => d.Version));
    }

    [Fact]
    public async Task SelectDesign_PersistsPreferenceWithoutChangingVersionHistory()
    {
        var workflowId = Guid.NewGuid();
        var selected = Design(workflowId, 1, false);
        var current = Design(workflowId, 2, true);
        _dbContext.WorkflowStates.Add(new WorkflowState
        {
            Id = workflowId, LandSubmissionId = Guid.NewGuid(), Status = "design_generated",
            HouseDesigns = [selected, current]
        });
        await _dbContext.SaveChangesAsync();

        Assert.IsType<OkObjectResult>(await _controller.SelectDesign(workflowId, selected.Id));
        _dbContext.ChangeTracker.Clear();
        var persisted = await _dbContext.WorkflowStates.Include(w => w.HouseDesigns).SingleAsync(w => w.Id == workflowId);

        Assert.Equal(selected.Id, persisted.PreferredHouseDesignId);
        Assert.Equal("selected_by_client", persisted.Status);
        Assert.Equal(2, persisted.HouseDesigns.Count);
        Assert.True(persisted.HouseDesigns.Single(d => d.Id == current.Id).IsCurrent);
    }

    [Fact]
    public async Task SelectDesign_RejectsDesignFromAnotherWorkflow()
    {
        var firstId = Guid.NewGuid(); var secondId = Guid.NewGuid();
        var foreign = Design(secondId, 1, true);
        _dbContext.WorkflowStates.AddRange(
            new WorkflowState { Id = firstId, LandSubmissionId = Guid.NewGuid(), Status = "design_generated" },
            new WorkflowState { Id = secondId, LandSubmissionId = Guid.NewGuid(), Status = "design_generated", HouseDesigns = [foreign] });
        await _dbContext.SaveChangesAsync();

        Assert.IsType<BadRequestObjectResult>(await _controller.SelectDesign(firstId, foreign.Id));
    }

    [Fact]
    public async Task SubmitArchitectReview_UsesPersistedSelectedDesign()
    {
        var clientId = Guid.NewGuid(); var submissionId = Guid.NewGuid(); var workflowId = Guid.NewGuid();
        var selected = Design(workflowId, 1, true);
        var submission = new LandSubmission
        {
            Id = submissionId, ClientId = clientId, LandSizePerches = 10,
            PreferredBedrooms = 3, PreferredFloors = 1, CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.LandSubmissions.Add(submission);
        _dbContext.WorkflowStates.Add(new WorkflowState
        {
            Id = workflowId, LandSubmissionId = submissionId, LandSubmission = submission,
            Status = "selected_by_client", PreferredHouseDesignId = selected.Id, HouseDesigns = [selected]
        });
        await _dbContext.SaveChangesAsync();

        Assert.IsType<OkObjectResult>(await _controller.SubmitArchitectReview(workflowId));
        var persisted = await _dbContext.WorkflowStates.SingleAsync(w => w.Id == workflowId);
        Assert.Equal("awaiting_architect_review", persisted.Status);
        Assert.Single(await _dbContext.ValidationRequests.Where(r => r.WorkflowStateId == workflowId).ToListAsync());
    }

    private static HouseDesign Design(Guid workflowId, int version, bool current) => new()
    {
        Id = Guid.NewGuid(), WorkflowStateId = workflowId, Version = version, IsCurrent = current,
        FloorCount = 1, TotalBuiltUpAreaSqft = 700, FoundationType = "slab",
        LayoutJson = """{"template_family":"COMPACT_RECTANGLE","geometry_fingerprint":"fp","candidate_summary":{"generation_mode":"deterministic_fallback","selected_plan_code":"BASE-1"},"rooms":[]}""",
        CreatedAt = DateTimeOffset.UtcNow.AddMinutes(version)
    };
}
