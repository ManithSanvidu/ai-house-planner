using System.ComponentModel.DataAnnotations;

namespace HousePlanner.API.Models;

public sealed class HouseRequirement
{
    [Required]
    public string LandSizeCategory { get; init; } = string.Empty;
    public int LandSizePerches { get; init; }
    public int Bedrooms { get; init; }
    public int Bathrooms { get; init; }
    [Required]
    public string HouseType { get; init; } = string.Empty;
    public int Floors => 1;
}

public sealed class StartDesignRequest
{
    [Required]
    public string LandSizeCategory { get; init; } = string.Empty;
    public int LandSizePerches { get; init; }
    public int Bedrooms { get; init; }
    public int Bathrooms { get; init; }
    [Required]
    public string HouseType { get; init; } = string.Empty;

    public HouseRequirement ToRequirement() => new()
    {
        LandSizeCategory = LandSizeCategory,
        LandSizePerches = LandSizePerches,
        Bedrooms = Bedrooms,
        Bathrooms = Bathrooms,
        HouseType = HouseType
    };
}
