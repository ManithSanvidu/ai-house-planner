using HousePlanner.API.DTOs;

namespace HousePlanner.API.Services;

public interface IDailyConstructionLogService
{
    Task<IEnumerable<DailyConstructionLogDto>> GetLogsAsync(Guid projectId, Guid constructorId, CancellationToken cancellationToken = default);
    Task<DailyConstructionLogDto?> GetLogAsync(Guid projectId, Guid logId, Guid constructorId, CancellationToken cancellationToken = default);
    Task<DailyConstructionLogDto> CreateLogAsync(Guid projectId, Guid constructorId, CreateDailyConstructionLogRequest request, CancellationToken cancellationToken = default);
    Task<DailyConstructionLogDto> UpdateLogAsync(Guid projectId, Guid logId, Guid constructorId, UpdateDailyConstructionLogRequest request, CancellationToken cancellationToken = default);
    Task DeleteLogAsync(Guid projectId, Guid logId, Guid constructorId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CalendarEventDto>> GetProjectCalendarAsync(Guid projectId, Guid constructorId, CancellationToken cancellationToken = default);
}
