using System.Text.Json.Serialization;

namespace HousePlanner.API.Models;

public record LandRangeDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("minPerches")] decimal MinPerches,
    [property: JsonPropertyName("maxPerches")] decimal MaxPerches,
    [property: JsonPropertyName("approxSqft")] string ApproxSqft
);

public record FeatureAvailabilityDto(
    [property: JsonPropertyName("available")] bool Available,
    [property: JsonPropertyName("reason")] string? Reason = null
);

public record DesignOptionsResponseDto(
    [property: JsonPropertyName("landRanges")] List<LandRangeDto> LandRanges,
    [property: JsonPropertyName("plotShapes")] List<string> PlotShapes,
    [property: JsonPropertyName("bedrooms")] List<int> Bedrooms,
    [property: JsonPropertyName("bathrooms")] List<int> Bathrooms,
    [property: JsonPropertyName("floors")] List<int> Floors,
    [property: JsonPropertyName("architecturalStyles")] List<string> ArchitecturalStyles,
    [property: JsonPropertyName("features")] Dictionary<string, FeatureAvailabilityDto> Features,
    [property: JsonPropertyName("validatedDesignCount")] int ValidatedDesignCount
);

public class DesignOptionsRequestDto
{
    [JsonPropertyName("landRangeId")]
    public string? LandRangeId { get; set; }

    [JsonPropertyName("plotShape")]
    public string? PlotShape { get; set; }

    [JsonPropertyName("floors")]
    public int? Floors { get; set; }

    [JsonPropertyName("bedrooms")]
    public int? Bedrooms { get; set; }

    [JsonPropertyName("bathrooms")]
    public int? Bathrooms { get; set; }

    [JsonPropertyName("architecturalStyle")]
    public string? ArchitecturalStyle { get; set; }
    public bool? OpenPlan { get; set; }
    public bool? MasterEnsuite { get; set; }
    public bool? SeparateDining { get; set; }
    public bool? HomeOffice { get; set; }
    public bool? Balcony { get; set; }
    public bool? Veranda { get; set; }
    public bool? UtilityRoom { get; set; }
    public bool? ParkingRequired { get; set; }
    public bool? Accessibility { get; set; }
}

public record SuggestionDto(
    [property: JsonPropertyName("field")] string Field,
    [property: JsonPropertyName("value")] object Value,
    [property: JsonPropertyName("label")] string Label
);

public class DesignOptionsValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorCode { get; set; }
    public string? Message { get; set; }
    public List<string> Conflicts { get; set; } = new();
    public List<SuggestionDto> Suggestions { get; set; } = new();
}
