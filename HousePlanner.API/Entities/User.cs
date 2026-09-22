using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HousePlanner.API.Entities
{
    [Table("Users")]
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Email { get; set; } = null!;

        /// <summary>Stable Supabase Authentication subject. This, rather than email, links an identity to application data.</summary>
        [StringLength(128)]
        public string? SupabaseUid { get; set; }

        // Retained only for legacy rows. Supabase users never have passwords or password hashes stored here.
        [StringLength(255)]
        public string? PasswordHash { get; set; }

        [Required]
        [StringLength(150)]
        public string FullName { get; set; } = null!;

        [Required]
        public int RoleId { get; set; }
        
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; } = null!;

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
