using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HousePlanner.API.Controllers;
using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
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
    private readonly Guid _clientId = Guid.NewGuid();

    public WorkflowControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<WorkflowController>>();
        var clients = new Mock<IHttpClientFactory>();
        clients.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
        var currentUser = new Mock<ICurrentUserContextService>();
        currentUser.Setup(x => x.GetAsync(It.IsAny<HttpContext>())).Returns(async () =>
        {
            var submissionIds = await _dbContext.LandSubmissions.Select(x => x.Id).ToListAsync();
            foreach (var id in await _dbContext.WorkflowStates.Select(x => x.LandSubmissionId).ToListAsync())
                if (!submissionIds.Contains(id))
                    _dbContext.LandSubmissions.Add(new LandSubmission
                    {
                        Id = id, ClientId = _clientId, LandSizePerches = 10,
                        PreferredBedrooms = 3, PreferredFloors = 1
                    });
            await _dbContext.SaveChangesAsync();
            return new CurrentUserContext(_clientId, "customer@example.com", "Customer");
        });
        _controller = new WorkflowController(_dbContext, _loggerMock.Object, clients.Object,
            Mock.Of<IWorkflowService>(), currentUser.Object);
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
            },
            CostEstimates = new List<CostEstimate>
            {
                new CostEstimate
                {
                    MaterialCostLkr = 500000m,
                    LabourCostLkr = 150000m,
                    TotalCostLkr = 650000m,
                    BudgetDeltaPercent = 65m
                }
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
        Assert.NotNull(response.Cost);
        Assert.Equal(500000m, response.Cost.MaterialCostLkr);
        Assert.Equal(150000m, response.Cost.LabourCostLkr);
        Assert.Equal(650000m, response.Cost.TotalCostLkr);
        Assert.Equal(65m, response.Cost.BudgetDeltaPercent);
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
    public async Task SelectDesign_ReplacesPreviousSelection_AndCanUnselect()
    {
        var workflowId = Guid.NewGuid(); var first = Design(workflowId, 1, false); var second = Design(workflowId, 2, true);
        _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = Guid.NewGuid(), Status = "design_generated", HouseDesigns = [first, second] });
        await _dbContext.SaveChangesAsync();

        await _controller.SelectDesign(workflowId, first.Id);
        await _controller.SelectDesign(workflowId, second.Id);
        Assert.Equal(second.Id, (await _dbContext.WorkflowStates.FindAsync(workflowId))!.PreferredHouseDesignId);
        Assert.IsType<OkObjectResult>(await _controller.ClearDesignSelection(workflowId));
        Assert.Null((await _dbContext.WorkflowStates.FindAsync(workflowId))!.PreferredHouseDesignId);
    }

    [Fact]
    public async Task RemoveDesign_ArchivesUnsubmittedVersion()
    {
        var workflowId = Guid.NewGuid(); var design = Design(workflowId, 1, true);
        _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = Guid.NewGuid(), Status = "design_generated", HouseDesigns = [design] });
        await _dbContext.SaveChangesAsync();

        Assert.IsType<OkObjectResult>(await _controller.RemoveDesign(workflowId, design.Id));
        Assert.True((await _dbContext.HouseDesigns.FindAsync(design.Id))!.IsArchived);
        var historyResult = Assert.IsType<OkObjectResult>(await _controller.GetDesigns(workflowId));
        Assert.True(Assert.Single(Assert.IsType<WorkflowDesignHistoryDto>(historyResult.Value).Designs).IsArchived);
    }

    [Fact]
    public async Task RemoveDesign_ClearsSelectionWhenSelected()
    {
        var workflowId = Guid.NewGuid(); var design = Design(workflowId, 1, true);
        _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = Guid.NewGuid(), Status = "selected_by_client", PreferredHouseDesignId = design.Id, HouseDesigns = [design] });
        await _dbContext.SaveChangesAsync();

        await _controller.RemoveDesign(workflowId, design.Id);
        Assert.Null((await _dbContext.WorkflowStates.FindAsync(workflowId))!.PreferredHouseDesignId);
    }

    [Fact]
    public async Task RemoveDesign_SubmittedVersionIsArchived()
    {
        var workflowId = Guid.NewGuid(); var design = Design(workflowId, 1, true);
        _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = Guid.NewGuid(), Status = "awaiting_architect_review", PreferredHouseDesignId = design.Id, HouseDesigns = [design] });
        await _dbContext.SaveChangesAsync();

        var result = Assert.IsType<OkObjectResult>(await _controller.RemoveDesign(workflowId, design.Id));
        Assert.Contains("archived", System.Text.Json.JsonSerializer.Serialize(result.Value));
        Assert.True((await _dbContext.HouseDesigns.FindAsync(design.Id))!.IsArchived);
    }

    [Fact]
    public async Task RemoveDesign_ApprovedVersionIsRejected()
    {
        var workflowId = Guid.NewGuid(); var design = Design(workflowId, 1, true);
        _dbContext.WorkflowStates.Add(new WorkflowState { Id = workflowId, LandSubmissionId = Guid.NewGuid(), Status = "approved", ApprovalStatus = "approved", PreferredHouseDesignId = design.Id, HouseDesigns = [design] });
        await _dbContext.SaveChangesAsync();

        Assert.IsType<ConflictObjectResult>(await _controller.RemoveDesign(workflowId, design.Id));
        Assert.False((await _dbContext.HouseDesigns.FindAsync(design.Id))!.IsArchived);
    }

    [Fact]
    public async Task SubmitArchitectReview_UsesPersistedSelectedDesign()
    {
        var clientId = _clientId; var submissionId = Guid.NewGuid(); var workflowId = Guid.NewGuid();
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

        Assert.IsType<OkObjectResult>(await _controller.SubmitArchitectReview(workflowId, selected.Id));
        var persisted = await _dbContext.WorkflowStates.SingleAsync(w => w.Id == workflowId);
        Assert.Equal("awaiting_architect_review", persisted.Status);
        var request=Assert.Single(await _dbContext.ValidationRequests.Where(r => r.WorkflowStateId == workflowId).ToListAsync());
        Assert.Equal(selected.Id,request.HouseDesignId);
        Assert.IsType<ConflictObjectResult>(await _controller.SubmitArchitectReview(workflowId, selected.Id));
    }

    private static HouseDesign Design(Guid workflowId, int version, bool current) => new()
    {
        Id = Guid.NewGuid(), WorkflowStateId = workflowId, Version = version, IsCurrent = current,
        FloorCount = 1, TotalBuiltUpAreaSqft = 700, FoundationType = "slab",
        LayoutJson = """{"template_family":"COMPACT_RECTANGLE","geometry_fingerprint":"fp","candidate_summary":{"generation_mode":"deterministic_fallback","selected_plan_code":"BASE-1"},"rooms":[]}""",
        CreatedAt = DateTimeOffset.UtcNow.AddMinutes(version)
    };

    [Fact]
    public async Task GetWorkflowStatus_CannotBeReadByDifferentCustomer()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        
        // This creates a workflow owned by some other user (not _clientId)
        _dbContext.LandSubmissions.Add(new LandSubmission
        {
            Id = submissionId, ClientId = Guid.NewGuid(), LandSizePerches = 10,
            PreferredBedrooms = 3, PreferredFloors = 1
        });
        _dbContext.WorkflowStates.Add(new WorkflowState
        {
            Id = workflowId,
            LandSubmissionId = submissionId,
            Status = "pending",
            TerrainType = "flat"
        });
        await _dbContext.SaveChangesAsync();

        // Act - _controller is configured with _clientId as the current user
        var result = await _controller.GetWorkflowStatus(workflowId);

        // Assert - The controller returns 404 because the ownership check fails
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
