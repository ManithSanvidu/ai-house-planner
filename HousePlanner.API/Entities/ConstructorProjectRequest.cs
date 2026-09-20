using System;
using System.ComponentModel.DataAnnotations;

namespace HousePlanner.API.Entities
{
    public class ConstructorProjectRequest
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ProjectId { get; set; }

        [Required]
        public Guid CustomerId { get; set; }

        [Required]
        public Guid HouseDesignId { get; set; }

        [Required]
        public Guid ConstructorId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Declined, Cancelled

        [MaxLength(1000)]
        public string? DeclineReason { get; set; }

        public DateTimeOffset? RespondedAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public Project? Project { get; set; }
        public User? Customer { get; set; }
        public User? Constructor { get; set; }
        public HouseDesign? HouseDesign { get; set; }
    }
}
