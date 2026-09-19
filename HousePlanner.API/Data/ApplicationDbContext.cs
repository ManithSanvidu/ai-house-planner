using Microsoft.EntityFrameworkCore;
using HousePlanner.API.Entities;

namespace HousePlanner.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<LandSubmission> LandSubmissions { get; set; }
        public DbSet<HouseDesign> HouseDesigns { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<WorkflowState> WorkflowStates { get; set; }
        public DbSet<ValidationRequest> ValidationRequests { get; set; }
        public DbSet<PreDesignedHousePlan> PreDesignedHousePlans { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ConstructionPhase> ConstructionPhases { get; set; } = null!;
        public DbSet<ConstructorWorkflowLog> ConstructorWorkflowLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed default roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "User" },
                new Role { Id = 2, Name = "Admin" }
            );

            modelBuilder.Entity<HouseDesign>(entity =>
            {
                entity.HasIndex(e => e.WorkflowStateId).HasDatabaseName("IX_HouseDesigns_WorkflowStateId");
                entity.HasIndex(e => new { e.WorkflowStateId, e.IsCurrent }).HasDatabaseName("IX_HouseDesigns_WorkflowState_IsCurrent");
                entity.HasIndex(e => new { e.WorkflowStateId, e.Version })
                    .IsUnique()
                    .HasDatabaseName("UX_HouseDesigns_WorkflowState_Version");
                try 
                {
                    entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
                } 
                catch 
                { 
                    // Ignore for in-memory provider 
                }
            });

            modelBuilder.Entity<Room>(entity =>
            {
                entity.HasIndex(e => e.HouseDesignId).HasDatabaseName("IX_Rooms_HouseDesignId");
            });

            modelBuilder.Entity<PreDesignedHousePlan>(entity =>
            {
                entity.HasIndex(e => e.Slug).IsUnique();
                entity.HasIndex(e => e.DesignCode).IsUnique();
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.Bedrooms);
                entity.HasIndex(e => e.Bathrooms);
                entity.HasIndex(e => e.FloorCount);
                entity.HasIndex(e => e.Style);
                entity.HasIndex(e => e.SuitableTerrain);
            });

            modelBuilder.Entity<HouseDesign>()
                .HasOne(e => e.BasePreDesignedPlan)
                .WithMany(e => e.DerivedDesigns)
                .HasForeignKey(e => e.BasePreDesignedPlanId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<LandSubmission>()
                .HasOne(x => x.BasePreDesignedPlan)
                .WithMany()
                .HasForeignKey(x => x.BasePreDesignedPlanId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}

