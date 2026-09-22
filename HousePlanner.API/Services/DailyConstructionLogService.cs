using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Services;

public class DailyConstructionLogService : IDailyConstructionLogService
{
    private readonly ApplicationDbContext _db;

    public DailyConstructionLogService(ApplicationDbContext db)
    {
        _db = db;
    }

    private static DailyConstructionLogDto MapToDto(DailyConstructionLog log) => new(
        Id: log.Id,
        ProjectId: log.ProjectId,
        LogDate: log.LogDate,
        ConstructionPhaseId: log.ConstructionPhaseId,
        PhaseName: log.ConstructionPhase?.PhaseName,
        WorkCompleted: log.WorkCompleted,
        Challenges: log.Challenges,
        MaterialsUsed: log.MaterialsUsed,
        WorkforceCount: log.WorkforceCount,
        WeatherCondition: log.WeatherCondition,
        SafetyIssues: log.SafetyIssues,
        ProgressPercentage: log.ProgressPercentage,
        TomorrowPlan: log.TomorrowPlan,
        Notes: log.Notes,
        CreatedAt: log.CreatedAt,
        UpdatedAt: log.UpdatedAt
    );

    public async Task<IEnumerable<DailyConstructionLogDto>> GetLogsAsync(Guid projectId, Guid constructorId, CancellationToken cancellationToken = default)
    {
        // Enforce ownership
        var isOwner = await _db.Projects.AnyAsync(p => p.Id == projectId && p.ContractorId == constructorId, cancellationToken);
        if (!isOwner) throw new UnauthorizedAccessException("Project not found or not owned by the current constructor.");

        var logs = await _db.DailyConstructionLogs
            .Include(l => l.ConstructionPhase)
            .Where(l => l.ProjectId == projectId)
            .OrderByDescending(l => l.LogDate)
            .ThenByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        return logs.Select(MapToDto);
    }

    public async Task<DailyConstructionLogDto?> GetLogAsync(Guid projectId, Guid logId, Guid constructorId, CancellationToken cancellationToken = default)
    {
        var log = await _db.DailyConstructionLogs
            .Include(l => l.ConstructionPhase)
            .Include(l => l.Project)
            .FirstOrDefaultAsync(l => l.Id == logId && l.ProjectId == projectId, cancellationToken);

        if (log == null) return null;
        if (log.Project?.ContractorId != constructorId) throw new UnauthorizedAccessException("Project not found or not owned by the current constructor.");

        return MapToDto(log);
    }

    public async Task<DailyConstructionLogDto> CreateLogAsync(Guid projectId, Guid constructorId, CreateDailyConstructionLogRequest request, CancellationToken cancellationToken = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project == null || project.ContractorId != constructorId)
            throw new UnauthorizedAccessException("Project not found or not owned by the current constructor.");

        if (project.Status.Equals("completed", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("project_completed_logbook_read_only");

        if (request.ConstructionPhaseId.HasValue)
        {
            var phase = await _db.ConstructionPhases.FirstOrDefaultAsync(p => p.Id == request.ConstructionPhaseId.Value && p.ProjectId == projectId, cancellationToken);
            if (phase == null) throw new ArgumentException("The specified construction phase does not exist on this project.");
        }

        var log = new DailyConstructionLog
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            ConstructorId = constructorId,
            LogDate = request.LogDate,
            ConstructionPhaseId = request.ConstructionPhaseId,
            WorkCompleted = request.WorkCompleted,
            Challenges = request.Challenges,
            MaterialsUsed = request.MaterialsUsed,
            WorkforceCount = request.WorkforceCount,
            WeatherCondition = request.WeatherCondition,
            SafetyIssues = request.SafetyIssues,
            ProgressPercentage = request.ProgressPercentage,
            TomorrowPlan = request.TomorrowPlan,
            Notes = request.Notes,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _db.DailyConstructionLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);

        // Load phase for DTO
        if (log.ConstructionPhaseId.HasValue)
        {
            await _db.Entry(log).Reference(l => l.ConstructionPhase).LoadAsync(cancellationToken);
        }

        return MapToDto(log);
    }

    public async Task<DailyConstructionLogDto> UpdateLogAsync(Guid projectId, Guid logId, Guid constructorId, UpdateDailyConstructionLogRequest request, CancellationToken cancellationToken = default)
    {
        var log = await _db.DailyConstructionLogs
            .Include(l => l.Project)
            .FirstOrDefaultAsync(l => l.Id == logId && l.ProjectId == projectId, cancellationToken);

        if (log == null) throw new KeyNotFoundException("Log not found.");
        if (log.Project?.ContractorId != constructorId) throw new UnauthorizedAccessException("Project not found or not owned by the current constructor.");

        if (log.Project.Status.Equals("completed", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("project_completed_logbook_read_only");

        if (request.ConstructionPhaseId.HasValue && request.ConstructionPhaseId != log.ConstructionPhaseId)
        {
            var phase = await _db.ConstructionPhases.FirstOrDefaultAsync(p => p.Id == request.ConstructionPhaseId.Value && p.ProjectId == projectId, cancellationToken);
            if (phase == null) throw new ArgumentException("The specified construction phase does not exist on this project.");
        }

        log.LogDate = request.LogDate;
        log.ConstructionPhaseId = request.ConstructionPhaseId;
        log.WorkCompleted = request.WorkCompleted;
        log.Challenges = request.Challenges;
        log.MaterialsUsed = request.MaterialsUsed;
        log.WorkforceCount = request.WorkforceCount;
        log.WeatherCondition = request.WeatherCondition;
        log.SafetyIssues = request.SafetyIssues;
        log.ProgressPercentage = request.ProgressPercentage;
        log.TomorrowPlan = request.TomorrowPlan;
        log.Notes = request.Notes;
        log.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        if (log.ConstructionPhaseId.HasValue)
        {
            await _db.Entry(log).Reference(l => l.ConstructionPhase).LoadAsync(cancellationToken);
        }

        return MapToDto(log);
    }

    public async Task DeleteLogAsync(Guid projectId, Guid logId, Guid constructorId, CancellationToken cancellationToken = default)
    {
        var log = await _db.DailyConstructionLogs
            .Include(l => l.Project)
            .FirstOrDefaultAsync(l => l.Id == logId && l.ProjectId == projectId, cancellationToken);

        if (log == null) throw new KeyNotFoundException("Log not found.");
        if (log.Project?.ContractorId != constructorId) throw new UnauthorizedAccessException("Project not found or not owned by the current constructor.");

        if (log.Project.Status.Equals("completed", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("project_completed_logbook_read_only");

        _db.DailyConstructionLogs.Remove(log);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<CalendarEventDto>> GetProjectCalendarAsync(Guid projectId, Guid constructorId, CancellationToken cancellationToken = default)
    {
        var isOwner = await _db.Projects.AnyAsync(p => p.Id == projectId && p.ContractorId == constructorId, cancellationToken);
        if (!isOwner) throw new UnauthorizedAccessException("Project not found or not owned by the current constructor.");

        var logs = await _db.DailyConstructionLogs
            .Include(l => l.ConstructionPhase)
            .Where(l => l.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        var events = new List<CalendarEventDto>();

        foreach (var log in logs)
        {
            var isIssue = !string.IsNullOrWhiteSpace(log.Challenges) || !string.IsNullOrWhiteSpace(log.SafetyIssues);
            
            // Log Event
            events.Add(new CalendarEventDto(
                Id: Guid.NewGuid(),
                Date: log.LogDate.ToDateTime(new TimeOnly(0, 0)),
                Title: isIssue ? "Issue / Delay" : "Work Log",
                Type: isIssue ? "Issue" : "Log",
                Status: isIssue ? "Delay" : "Active",
                Description: log.WorkCompleted,
                ProjectId: projectId,
                DailyLogId: log.Id,
                PhaseId: log.ConstructionPhaseId,
                Color: isIssue ? "#ef4444" : "#3b82f6" // Red or Blue
            ));

            // Tomorrow's Plan (Optional extra event)
            if (!string.IsNullOrWhiteSpace(log.TomorrowPlan))
            {
                events.Add(new CalendarEventDto(
                    Id: Guid.NewGuid(),
                    Date: log.LogDate.AddDays(1).ToDateTime(new TimeOnly(0, 0)),
                    Title: "Planned Work",
                    Type: "Planned",
                    Status: "Planned",
                    Description: log.TomorrowPlan,
                    ProjectId: projectId,
                    DailyLogId: log.Id,
                    PhaseId: log.ConstructionPhaseId,
                    Color: "#8b5cf6" // Purple
                ));
            }
        }

        return events.OrderBy(e => e.Date);
    }
}
