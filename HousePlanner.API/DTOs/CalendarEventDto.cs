using System;

namespace HousePlanner.API.DTOs;

public record CalendarEventDto(
    Guid Id,
    DateTime Date, // Standardized for FullCalendar
    string Title,
    string Type, // "Log", "PhaseStart", "PhaseEnd", "Issue", "Milestone", "Planned"
    string Status, // "Active", "Delay", "Planned", "Completed"
    string Description,
    Guid ProjectId,
    Guid? DailyLogId,
    Guid? PhaseId,
    string Color // Pre-calculated hex color for the frontend
);
