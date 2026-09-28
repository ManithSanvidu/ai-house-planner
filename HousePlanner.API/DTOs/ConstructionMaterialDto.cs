using System;

namespace HousePlanner.API.DTOs
{
    public class ConstructionMaterialDto
    {
        public Guid Id { get; set; }
        public Guid? ProjectId { get; set; }
        public Guid? HouseDesignId { get; set; }
        public Guid? PhaseId { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public decimal RequiredQuantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal AvailableQuantity { get; set; }
        public decimal OrderedQuantity { get; set; }
        public string? Supplier { get; set; }
        public DateTimeOffset? ExpectedDeliveryDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CreateConstructionMaterialDto
    {
        public Guid? ProjectId { get; set; }
        public Guid? HouseDesignId { get; set; }
        public Guid? PhaseId { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public decimal RequiredQuantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal AvailableQuantity { get; set; }
        public decimal OrderedQuantity { get; set; }
        public string? Supplier { get; set; }
        public DateTimeOffset? ExpectedDeliveryDate { get; set; }
        public string Status { get; set; } = "Pending";
    }
}
