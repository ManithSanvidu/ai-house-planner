using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace HousePlanner.API.DTOs;

public record PreDesignedPlanSummaryDto(Guid Id, string Name, string Slug, string DesignCode,
    string Style, int Bedrooms, int Bathrooms, int FloorCount, decimal TotalBuiltUpAreaSqft,
    decimal MinimumLandSizePerches, string SuitableTerrain, int ParkingSpaces, bool HasBalcony,
    bool HasVeranda, bool HasOffice, bool IsAccessibleFriendly, string? Category,
    IReadOnlyList<string> Tags, string? ThumbnailUrl, bool IsActive, DateTimeOffset UpdatedAt);

public record PreDesignedPlanDetailDto(Guid Id, string Name, string Slug, string DesignCode,
    string? Description, string Style, int Bedrooms, int Bathrooms, int FloorCount,
    decimal TotalBuiltUpAreaSqft, decimal MinimumLandSizePerches, decimal? MinimumPlotWidthFt,
    decimal? MinimumPlotLengthFt, string SuitableTerrain, int ParkingSpaces, bool HasBalcony,
    bool HasVeranda, bool HasOffice, bool HasUtilityRoom, bool IsAccessibleFriendly,
    string? Category, IReadOnlyList<string> Tags, string? ThumbnailUrl, JsonElement Layout,
    bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string ConceptualDisclaimer);

public class SavePreDesignedPlanDto
{
    [Required, MaxLength(160)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(180)] public string Slug { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string DesignCode { get; set; } = string.Empty;
    [MaxLength(2000)] public string? Description { get; set; }
    [Required, MaxLength(80)] public string Style { get; set; } = string.Empty;
    [Range(1, 20)] public int Bedrooms { get; set; }
    [Range(1, 20)] public int Bathrooms { get; set; }
    [Range(1, 5)] public int FloorCount { get; set; }
    [Range(1, 100000)] public decimal TotalBuiltUpAreaSqft { get; set; }
    [Range(0.1, 10000)] public decimal MinimumLandSizePerches { get; set; }
    public decimal? MinimumPlotWidthFt { get; set; }
    public decimal? MinimumPlotLengthFt { get; set; }
    [Required] public string SuitableTerrain { get; set; } = "flat";
    [Range(0, 20)] public int ParkingSpaces { get; set; }
    public bool HasBalcony { get; set; }
    public bool HasVeranda { get; set; }
    public bool HasOffice { get; set; }
    public bool HasUtilityRoom { get; set; }
    public bool IsAccessibleFriendly { get; set; }
    public string? Category { get; set; }
    public string[] Tags { get; set; } = [];
    public string? ThumbnailUrl { get; set; }
    public JsonElement Layout { get; set; }
    public bool IsActive { get; set; } = true;
}

public record CompatibilityRequest(decimal LandSizePerches, decimal? PlotWidthFt,
    decimal? PlotLengthFt, string Terrain, int? PreferredBedrooms, int? PreferredFloors);
public record CompatibilityResponse(bool Compatible, IReadOnlyList<string> Issues,
    IReadOnlyList<string> Warnings);
