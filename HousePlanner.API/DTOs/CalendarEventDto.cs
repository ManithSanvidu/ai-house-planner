using System;

namespace HousePlanner.API.DTOs;

public record CalendarEventDto(
    Guid Id,
    DateOnly Date,
    string Title,
    string Type,
    string Status,
    string? Description,
    Guid ProjectId,
    Guid? DailyLogId,
    Guid? PhaseId
);
