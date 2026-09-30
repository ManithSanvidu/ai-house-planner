using System.Text.Json;

namespace HousePlanner.API.DTOs;

public class CreateDesignValidationReportRequest
{
    public bool GeometryPassed { get; set; }

    public List<string> GeometryFailures { get; set; } = new();
    
    public List<string> GeometryFailedRules { get; set; } = new();

    public bool BusinessPassed { get; set; }

    public JsonElement BusinessRules { get; set; }

    public int AttemptNumber { get; set; } = 1;

    public string? ValidationSummary { get; set; }

    public string? ValidationSourceVersion { get; set; }
}
