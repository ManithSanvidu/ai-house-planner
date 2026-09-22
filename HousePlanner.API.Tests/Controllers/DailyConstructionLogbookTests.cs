using System.Text.Json;
using HousePlanner.API.Controllers;
using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace HousePlanner.API.Tests.Controllers;

public class DailyConstructionLogbookTests
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<ICurrentUserContextService> _mockUser;
    private readonly ConstructorLogbookController _controller;
    private readonly DailyConstructionLogService _service;

    public DailyConstructionLogbookTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);

        _mockUser = new Mock<ICurrentUserContextService>();
        _service = new DailyConstructionLogService(_db);
        _controller = new ConstructorLogbookController(_service, _mockUser.Object);
    }

    private void SetUser(Guid id, string role)
    {
        _mockUser.Setup(x => x.GetAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(new CurrentUserContext(id, role, "test@test.com"));
    }

    private async Task<(Guid constructorId, Guid projectId, Guid phaseId)> SeedProjectAsync(string status = "active")
    {
        var constructorId = Guid.NewGuid();
        var workflow = new WorkflowState { Id = Guid.NewGuid(), Status = "running", ApprovalStatus = "not_requested", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
        _db.WorkflowStates.Add(workflow);

        var project = new Project { Id = Guid.NewGuid(), WorkflowStateId = workflow.Id, ContractorId = constructorId, Status = status, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
        var phase = new ConstructionPhase { Id = Guid.NewGuid(), ProjectId = project.Id, PhaseName = "Phase 1", SequenceOrder = 1, Status = "pending", EstimatedDurationDays = 7 };
        project.ConstructionPhases.Add(phase);
        
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        return (constructorId, project.Id, phase.Id);
    }

    [Fact]
    public async Task Constructor_CanCreateLogForOwnProject()
    {
        var (constructorId, projectId, phaseId) = await SeedProjectAsync();
        SetUser(constructorId, "Constructor");

        var req = new CreateDailyConstructionLogRequest { LogDate = DateOnly.FromDateTime(DateTime.UtcNow), WorkCompleted = "Test", ConstructionPhaseId = phaseId, ProgressPercentage = 10 };
        var result = await _controller.CreateLog(projectId, req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<DailyConstructionLogDto>(ok.Value);
        Assert.Equal("Test", dto.WorkCompleted);
        Assert.Equal(1, await _db.DailyConstructionLogs.CountAsync());
    }

    [Fact]
    public async Task Constructor_CannotCreateLogForOtherConstructorProject()
    {
        var (_, projectId, phaseId) = await SeedProjectAsync();
        SetUser(Guid.NewGuid(), "Constructor"); // Different constructor

        var req = new CreateDailyConstructionLogRequest { LogDate = DateOnly.FromDateTime(DateTime.UtcNow), WorkCompleted = "Test", ConstructionPhaseId = phaseId };
        var result = await _controller.CreateLog(projectId, req, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, status.StatusCode);
    }

    [Fact]
    public async Task Constructor_CanReadOwnProjectLogs()
    {
        var (constructorId, projectId, phaseId) = await SeedProjectAsync();
        SetUser(constructorId, "Constructor");

        await _service.CreateLogAsync(projectId, constructorId, new CreateDailyConstructionLogRequest { LogDate = DateOnly.FromDateTime(DateTime.UtcNow), WorkCompleted = "Test1" });
        await _service.CreateLogAsync(projectId, constructorId, new CreateDailyConstructionLogRequest { LogDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1), WorkCompleted = "Test2" });

        var result = await _controller.GetLogs(projectId, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        var logs = Assert.IsAssignableFrom<IEnumerable<DailyConstructionLogDto>>(ok.Value);
        Assert.Equal(2, logs.Count());
    }

    [Fact]
    public async Task Constructor_CannotReadOtherConstructorLogs()
    {
        var (constructorId, projectId, phaseId) = await SeedProjectAsync();
        await _service.CreateLogAsync(projectId, constructorId, new CreateDailyConstructionLogRequest { LogDate = DateOnly.FromDateTime(DateTime.UtcNow), WorkCompleted = "Test" });

        SetUser(Guid.NewGuid(), "Constructor"); // Different
        var result = await _controller.GetLogs(projectId, CancellationToken.None);
        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, status.StatusCode);
    }

    [Fact]
    public async Task Constructor_CanUpdateOwnLog()
    {
        var (constructorId, projectId, phaseId) = await SeedProjectAsync();
        SetUser(constructorId, "Constructor");

        var log = await _service.CreateLogAsync(projectId, constructorId, new CreateDailyConstructionLogRequest { LogDate = DateOnly.FromDateTime(DateTime.UtcNow), WorkCompleted = "Test1" });

        var updateReq = new UpdateDailyConstructionLogRequest { LogDate = DateOnly.FromDateTime(DateTime.UtcNow), WorkCompleted = "Updated", ProgressPercentage = 50 };
        var result = await _controller.UpdateLog(projectId, log.Id, updateReq, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var updatedDto = Assert.IsType<DailyConstructionLogDto>(ok.Value);
        Assert.Equal("Updated", updatedDto.WorkCompleted);
        Assert.Equal(50, updatedDto.ProgressPercentage);
    }

    [Fact]
    public async Task Constructor_CannotUpdateOtherConstructorLog()
    {
        var (constructorId, projectId, phaseId) = await SeedProjectAsync();
        var log = await _service.CreateLogAsync(projectId, constructorId, new CreateDailyConstructionLogRequest { LogDate = DateOnly.FromDateTime(DateTime.UtcNow), WorkCompleted = "Test1" });

        SetUser(Guid.NewGuid(), "Constructor"); // Different
        var updateReq = new UpdateDailyConstructionLogRequest { LogDate = DateOnly.FromDateTime(DateTime.UtcNow), WorkCompleted = "Updated" };
        var result = await _controller.UpdateLog(projectId, log.Id, updateReq, CancellationToken.None);
        
        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, status.StatusCode);
    }

    [Fact]
    public async Task Constructor_CanDeleteOwnLog()
    {
        var (constructorId, projectId, phaseId) = await SeedProjectAsync();
        SetUser(constructorId, "Constructor");

        var log = await _service.CreateLogAsync(projectId, constructorId, new CreateDailyConstructionLogRequest { LogDate = DateOnly.FromDateTime(DateTime.UtcNow), WorkCompleted = "Test1" });

        var result = await _controller.DeleteLog(projectId, log.Id, CancellationToken.None);
        Assert.IsType<NoContentResult>(result);
        
        Assert.Empty(await _db.DailyConstructionLogs.ToListAsync());
    }

    [Fact]
    public async Task CompletedProject_LogbookIsReadOnly()
    {
        var (constructorId, projectId, phaseId) = await SeedProjectAsync("completed");
        SetUser(constructorId, "Constructor");

        var req = new CreateDailyConstructionLogRequest { LogDate = DateOnly.FromDateTime(DateTime.UtcNow), WorkCompleted = "Test" };
        var result = await _controller.CreateLog(projectId, req, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(409, status.StatusCode);
        Assert.Equal("project_completed_logbook_read_only", status.Value);
    }

    [Fact]
    public async Task CreateLog_RejectsPhaseFromDifferentProject()
    {
        var (constructorId1, projectId1, phaseId1) = await SeedProjectAsync();
        var (constructorId2, projectId2, phaseId2) = await SeedProjectAsync();
        SetUser(constructorId1, "Constructor");

        var req = new CreateDailyConstructionLogRequest { LogDate = DateOnly.FromDateTime(DateTime.UtcNow), WorkCompleted = "Test", ConstructionPhaseId = phaseId2 }; // Using phase from project 2
        var result = await _controller.CreateLog(projectId1, req, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("does not exist on this project", badRequest.Value.ToString());
    }

    [Fact]
    public async Task NoRawEntityCycleInLogResponse()
    {
        var (constructorId, projectId, phaseId) = await SeedProjectAsync();
        SetUser(constructorId, "Constructor");

        var log = await _service.CreateLogAsync(projectId, constructorId, new CreateDailyConstructionLogRequest { LogDate = DateOnly.FromDateTime(DateTime.UtcNow), WorkCompleted = "Test1", ConstructionPhaseId = phaseId });
        
        var result = await _controller.GetLog(projectId, log.Id, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        
        var json = JsonSerializer.Serialize(ok.Value);
        Assert.DoesNotContain("\"Project\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"Constructor\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"PhaseName\":", json, StringComparison.OrdinalIgnoreCase); // Ensured by mapping
    }

    [Fact]
    public async Task Constructor_CanRetrieveCalendarEvents()
    {
        var (constructorId, projectId, phaseId) = await SeedProjectAsync();
        SetUser(constructorId, "Constructor");

        // Create a log with challenges (should be mapped to an Issue/Delay event)
        var log1 = await _service.CreateLogAsync(projectId, constructorId, new CreateDailyConstructionLogRequest { 
            LogDate = DateOnly.FromDateTime(DateTime.UtcNow), 
            WorkCompleted = "Found an issue", 
            Challenges = "Rain delay",
            ConstructionPhaseId = phaseId 
        });

        // Create a normal log with tomorrow plan (should generate 2 events: log + planned)
        var log2 = await _service.CreateLogAsync(projectId, constructorId, new CreateDailyConstructionLogRequest { 
            LogDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), 
            WorkCompleted = "Normal work", 
            TomorrowPlan = "More work",
            ConstructionPhaseId = phaseId 
        });

        var result = await _controller.GetCalendar(projectId, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        var events = Assert.IsAssignableFrom<IEnumerable<HousePlanner.API.DTOs.CalendarEventDto>>(ok.Value);

        Assert.Equal(3, events.Count()); // 1 issue log, 1 normal log, 1 planned event

        var issueEvent = events.Single(e => e.Type == "Issue");
        Assert.Equal("Issue / Delay", issueEvent.Title);
        Assert.Equal("#ef4444", issueEvent.Color);

        var plannedEvent = events.Single(e => e.Type == "Planned");
        Assert.Equal("Planned Work", plannedEvent.Title);
        Assert.Equal("More work", plannedEvent.Description);
        Assert.Equal("#8b5cf6", plannedEvent.Color);
    }
}
