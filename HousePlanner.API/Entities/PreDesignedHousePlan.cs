using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HousePlanner.API.Entities;

[Table("PreDesignedHousePlans")]
public class PreDesignedHousePlan
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(160)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(180)] public string Slug { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string DesignCode { get; set; } = string.Empty;
    [MaxLength(2000)] public string? Description { get; set; }
    [Required, MaxLength(80)] public string Style { get; set; } = string.Empty;
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public int FloorCount { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal TotalBuiltUpAreaSqft { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal MinimumLandSizePerches { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal? MinimumPlotWidthFt { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal? MinimumPlotLengthFt { get; set; }
    [Required, MaxLength(30)] public string SuitableTerrain { get; set; } = "flat";
    public int ParkingSpaces { get; set; }
    public bool HasBalcony { get; set; }
    public bool HasVeranda { get; set; }
    public bool HasOffice { get; set; }
    public bool HasUtilityRoom { get; set; }
    public bool IsAccessibleFriendly { get; set; }
    [MaxLength(80)] public string? Category { get; set; }
    [Column(TypeName = "jsonb")] public string TagsJson { get; set; } = "[]";
    [MaxLength(500)] public string? ThumbnailUrl { get; set; }
    [Required, Column(TypeName = "jsonb")] public string LayoutJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<HouseDesign> DerivedDesigns { get; set; } = new List<HouseDesign>();
}
