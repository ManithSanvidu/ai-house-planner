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
        var phase = new ConstructionPhase { Id = Guid.NewGuid(), ProjectId = project.Id, PhaseName = "Phase 1", SequenceOrder = 1, Status = "pending", PlannedDurationDays = 7 };
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

        var status = Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetLogs_OwnProject_WithLogs_Returns200()
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
    public async Task GetLogs_OwnProject_NoLogs_Returns200EmptyArray()
    {
        var (constructorId, projectId, _) = await SeedProjectAsync();
        SetUser(constructorId, "Constructor");

        var result = await _controller.GetLogs(projectId, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        var logs = Assert.IsAssignableFrom<IEnumerable<DailyConstructionLogDto>>(ok.Value);
        Assert.Empty(logs);
    }

    [Fact]
    public async Task GetLogs_OtherConstructorProject_IsRejected()
    {
        var (constructorId, projectId, phaseId) = await SeedProjectAsync();
        await _service.CreateLogAsync(projectId, constructorId, new CreateDailyConstructionLogRequest { LogDate = DateOnly.FromDateTime(DateTime.UtcNow), WorkCompleted = "Test" });

        SetUser(Guid.NewGuid(), "Constructor"); // Different
        var result = await _controller.GetLogs(projectId, CancellationToken.None);
        var status = Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetLogs_MissingProject_Returns404()
    {
        SetUser(Guid.NewGuid(), "Constructor");

        var result = await _controller.GetLogs(Guid.NewGuid(), CancellationToken.None);
        var status = Assert.IsType<NotFoundResult>(result);
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

        var status = Assert.IsType<NotFoundResult>(result);
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
        Assert.NotNull(badRequest.Value);
        Assert.Contains("does not exist on this project", badRequest.Value.ToString()!);
    }

    [Fact]
    public async Task GetLogs_DoesNotReturnRawEntityCycle()
    {
        var (constructorId, projectId, phaseId) = await SeedProjectAsync();
        SetUser(constructorId, "Constructor");

        var log = await _service.CreateLogAsync(projectId, constructorId, new CreateDailyConstructionLogRequest { LogDate = DateOnly.FromDateTime(DateTime.UtcNow), WorkCompleted = "Test1", ConstructionPhaseId = phaseId });

        var result = await _controller.GetLog(projectId, log.Id, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);

        var json = JsonSerializer.Serialize(ok.Value);
        Assert.DoesNotContain("\"Project\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"Constructor\":", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetProjectCalendar_DailyLogsAppearOnCorrectDates()
    {
        var (constructorId, projectId, phaseId) = await SeedProjectAsync();
        SetUser(constructorId, "Constructor");

        var logDate = DateOnly.FromDateTime(DateTime.UtcNow);
        await _service.CreateLogAsync(projectId, constructorId, new CreateDailyConstructionLogRequest
        {
            LogDate = logDate,
            WorkCompleted = "Test log",
            Challenges = "Some issue",
            ConstructionPhaseId = phaseId
        });

        var events = await _service.GetProjectCalendarAsync(projectId, constructorId, CancellationToken.None);

        var logEvent = events.FirstOrDefault(e => e.Type == "daily_log");
        Assert.NotNull(logEvent);
        Assert.Equal(logDate, logEvent.Date);
        Assert.Equal("issue", logEvent.Status);
    }

    [Fact]
    public async Task GetProjectCalendar_DoesNotReturnRawEntities()
    {
        var (constructorId, projectId, phaseId) = await SeedProjectAsync();
        SetUser(constructorId, "Constructor");

        await _service.CreateLogAsync(projectId, constructorId, new CreateDailyConstructionLogRequest
        {
            LogDate = DateOnly.FromDateTime(DateTime.UtcNow),
            WorkCompleted = "Test log",
            ConstructionPhaseId = phaseId
        });

        var events = await _service.GetProjectCalendarAsync(projectId, constructorId, CancellationToken.None);

        var json = JsonSerializer.Serialize(events);
        Assert.DoesNotContain("\"Project\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"Constructor\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"ConstructionPhase\":", json, StringComparison.OrdinalIgnoreCase);
    }
}
