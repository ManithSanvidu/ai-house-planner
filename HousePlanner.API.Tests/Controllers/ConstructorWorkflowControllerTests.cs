using System.Text.Json;
using HousePlanner.API.Controllers;
using HousePlanner.API.Data;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace HousePlanner.API.Tests.Controllers;

public class ConstructorWorkflowControllerTests
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<ICurrentUserContextService> _mockUser;
    private readonly ConstructorWorkflowController _controller;

    public ConstructorWorkflowControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);

        _mockUser = new Mock<ICurrentUserContextService>();
        var mockLogService = new Mock<IDailyConstructionLogService>();
        var service = new ConstructorWorkflowService(_db);
        _controller = new ConstructorWorkflowController(service, _mockUser.Object, _db, mockLogService.Object);
    }

    private void SetUser(Guid id, string role)
    {
        _mockUser.Setup(x => x.GetAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(new CurrentUserContext(id, role, "test@test.com"));
    }

    [Fact]
    public async Task GetProjects_DoesNotSerializeEntityCycle()
    {
        var constructorId = Guid.NewGuid();
        SetUser(constructorId, "Constructor");

        var workflow = new WorkflowState { Id = Guid.NewGuid(), Status = "running", ApprovalStatus = "not_requested", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
        _db.WorkflowStates.Add(workflow);

        var project = new Project { Id = Guid.NewGuid(), WorkflowStateId = workflow.Id, ContractorId = constructorId, Status = "active", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
        project.ConstructionPhases.Add(new ConstructionPhase { Id = Guid.NewGuid(), ProjectId = project.Id, PhaseName = "Foundation", SequenceOrder = 1, Status = "pending", EstimatedDurationDays = 14 });
        
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        var result = await _controller.GetProjects();

        var okResult = Assert.IsType<OkObjectResult>(result);
        
        // Ensure it doesn't throw a JSON cycle exception when serialized
        var json = JsonSerializer.Serialize(okResult.Value);
        
        Assert.DoesNotContain("\"Project\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"project\":", json, StringComparison.OrdinalIgnoreCase);
        
        using var doc = JsonDocument.Parse(json);
        var array = doc.RootElement;
        Assert.Equal(JsonValueKind.Array, array.ValueKind);
        Assert.Equal(1, array.GetArrayLength());
        
        var firstProject = array[0];
        Assert.Equal(project.Id, firstProject.GetProperty("Id").GetGuid());
        Assert.True(firstProject.TryGetProperty("ConstructionPhases", out var phases));
        Assert.Equal(1, phases.GetArrayLength());
    }

    [Fact]
    public async Task GetProjects_ConstructorOnlySeesOwnProjects()
    {
        var constructorA = Guid.NewGuid();
        var constructorB = Guid.NewGuid();
        
        var workflow = new WorkflowState { Id = Guid.NewGuid(), Status = "running", ApprovalStatus = "not_requested", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
        _db.WorkflowStates.Add(workflow);

        var projectA = new Project { Id = Guid.NewGuid(), WorkflowStateId = workflow.Id, ContractorId = constructorA, Status = "active", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
        var projectB = new Project { Id = Guid.NewGuid(), WorkflowStateId = workflow.Id, ContractorId = constructorB, Status = "active", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
        
        _db.Projects.AddRange(projectA, projectB);
        await _db.SaveChangesAsync();

        SetUser(constructorA, "Constructor");
        var resultA = Assert.IsType<OkObjectResult>(await _controller.GetProjects());
        var docsA = JsonDocument.Parse(JsonSerializer.Serialize(resultA.Value)).RootElement;
        Assert.Equal(1, docsA.GetArrayLength());
        Assert.Equal(projectA.Id, docsA[0].GetProperty("Id").GetGuid());

        SetUser(constructorB, "Constructor");
        var resultB = Assert.IsType<OkObjectResult>(await _controller.GetProjects());
        var docsB = JsonDocument.Parse(JsonSerializer.Serialize(resultB.Value)).RootElement;
        Assert.Equal(1, docsB.GetArrayLength());
        Assert.Equal(projectB.Id, docsB[0].GetProperty("Id").GetGuid());
    }

    [Fact]
    public async Task GetProjects_NoProjects_ReturnsEmptyArray()
    {
        var constructorId = Guid.NewGuid();
        SetUser(constructorId, "Constructor");

        var result = await _controller.GetProjects();
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }
    [Fact]
    public async Task GetProjectCalendar_OwnProject_Returns200()
    {
        var projectId = Guid.NewGuid();
        var constructorId = Guid.NewGuid();
        SetUser(constructorId, "Constructor");

        var mockLogService = new Mock<IDailyConstructionLogService>();
        var events = new List<HousePlanner.API.DTOs.CalendarEventDto> 
        {
            new HousePlanner.API.DTOs.CalendarEventDto(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), "Test", "daily_log", "normal", null, projectId, null, null)
        };
        mockLogService.Setup(x => x.GetProjectCalendarAsync(projectId, constructorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        var controller = new ConstructorWorkflowController(new ConstructorWorkflowService(_db), _mockUser.Object, _db, mockLogService.Object);
        var result = await controller.GetProjectCalendar(projectId, CancellationToken.None);
        
        var ok = Assert.IsType<OkObjectResult>(result);
        var returnedEvents = Assert.IsAssignableFrom<IEnumerable<HousePlanner.API.DTOs.CalendarEventDto>>(ok.Value);
        Assert.Single(returnedEvents);
    }

    [Fact]
    public async Task GetProjectCalendar_NoEvents_ReturnsEmptyArray()
    {
        var projectId = Guid.NewGuid();
        var constructorId = Guid.NewGuid();
        SetUser(constructorId, "Constructor");

        var mockLogService = new Mock<IDailyConstructionLogService>();
        mockLogService.Setup(x => x.GetProjectCalendarAsync(projectId, constructorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HousePlanner.API.DTOs.CalendarEventDto>());

        var controller = new ConstructorWorkflowController(new ConstructorWorkflowService(_db), _mockUser.Object, _db, mockLogService.Object);
        var result = await controller.GetProjectCalendar(projectId, CancellationToken.None);
        
        var ok = Assert.IsType<OkObjectResult>(result);
        var returnedEvents = Assert.IsAssignableFrom<IEnumerable<HousePlanner.API.DTOs.CalendarEventDto>>(ok.Value);
        Assert.Empty(returnedEvents);
    }

    [Fact]
    public async Task GetProjectCalendar_OtherConstructorProject_IsRejected()
    {
        var projectId = Guid.NewGuid();
        var constructorId = Guid.NewGuid();
        SetUser(constructorId, "Constructor");

        var mockLogService = new Mock<IDailyConstructionLogService>();
        mockLogService.Setup(x => x.GetProjectCalendarAsync(projectId, constructorId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Project not found or not owned by the current constructor."));

        var controller = new ConstructorWorkflowController(new ConstructorWorkflowService(_db), _mockUser.Object, _db, mockLogService.Object);
        var result = await controller.GetProjectCalendar(projectId, CancellationToken.None);
        
        Assert.IsType<NotFoundResult>(result);
    }
}
