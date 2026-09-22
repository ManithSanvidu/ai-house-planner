using System.ComponentModel.DataAnnotations;

namespace HousePlanner.API.DTOs;

public record DailyConstructionLogDto(
    Guid Id,
    Guid ProjectId,
    DateOnly LogDate,
    Guid? ConstructionPhaseId,
    string? PhaseName,
    string WorkCompleted,
    string? Challenges,
    string? MaterialsUsed,
    int? WorkforceCount,
    string? WeatherCondition,
    string? SafetyIssues,
    decimal? ProgressPercentage,
    string? TomorrowPlan,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public class CreateDailyConstructionLogRequest
{
    [Required]
    public DateOnly LogDate { get; set; }

    public Guid? ConstructionPhaseId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "WorkCompleted cannot be empty.")]
    public string WorkCompleted { get; set; } = string.Empty;

    public string? Challenges { get; set; }
    public string? MaterialsUsed { get; set; }

    [Range(0, 10000, ErrorMessage = "WorkforceCount must be greater than or equal to 0.")]
    public int? WorkforceCount { get; set; }
    public string? WeatherCondition { get; set; }
    public string? SafetyIssues { get; set; }

    [Range(0, 100, ErrorMessage = "ProgressPercentage must be between 0 and 100.")]
    public decimal? ProgressPercentage { get; set; }

    public string? TomorrowPlan { get; set; }
    public string? Notes { get; set; }
}

public class UpdateDailyConstructionLogRequest
{
    [Required]
    public DateOnly LogDate { get; set; }

    public Guid? ConstructionPhaseId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "WorkCompleted cannot be empty.")]
    public string WorkCompleted { get; set; } = string.Empty;

    public string? Challenges { get; set; }
    public string? MaterialsUsed { get; set; }

    [Range(0, 10000, ErrorMessage = "WorkforceCount must be greater than or equal to 0.")]
    public int? WorkforceCount { get; set; }
    public string? WeatherCondition { get; set; }
    public string? SafetyIssues { get; set; }

    [Range(0, 100, ErrorMessage = "ProgressPercentage must be between 0 and 100.")]
    public decimal? ProgressPercentage { get; set; }

    public string? TomorrowPlan { get; set; }
    public string? Notes { get; set; }
}
