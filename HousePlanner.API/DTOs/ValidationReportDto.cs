using System.Text.Json;

namespace HousePlanner.API.DTOs
{
    public class ValidationReportDto
    {
        public Guid ValidationReportId { get; set; }
        public Guid HouseDesignId { get; set; }
        public int DesignVersion { get; set; }
        public bool OverallPassed { get; set; }
        public bool GeometryPassed { get; set; }
        public bool BusinessPassed { get; set; }
        public int AttemptNumber { get; set; }
        public DateTimeOffset ValidatedAt { get; set; }
        public string ValidationSummary { get; set; } = string.Empty;
        public string ValidationSourceVersion { get; set; } = string.Empty;

        // Deserialized JSON structures
        public JsonElement? GeometryFailures { get; set; }
        public JsonElement? GeometryFailedRules { get; set; }
        public JsonElement? BusinessRules { get; set; }

        // Design summary
        public int Bedrooms { get; set; }
        public int Floors { get; set; }
        public decimal TotalBuiltUpAreaSqft { get; set; }
        public string FoundationType { get; set; } = string.Empty;
        public string? TerrainType { get; set; }
    }
}
