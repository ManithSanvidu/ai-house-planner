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
        public DbSet<ConstructionPhase> ConstructionPhases { get; set; }
        public DbSet<ConstructorWorkflowLog> ConstructorWorkflowLogs { get; set; }
        public DbSet<ConstructorProjectRequest> ConstructorProjectRequests { get; set; }
        public DbSet<PricingData> PricingItems { get; set; }
        public DbSet<PricingImportAudit> PricingImportAudits { get; set; }
        public DbSet<CostEstimate> CostEstimates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed default roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "User" },
                new Role { Id = 2, Name = "Admin" }
            );

            modelBuilder.Entity<User>()
                .HasIndex(e => e.FirebaseUid)
                .IsUnique()
                .HasDatabaseName("UX_Users_FirebaseUid");

            modelBuilder.Entity<PricingData>()
                .OwnsOne(p => p.TerrainMultiplier, owned => owned.ToJson());

            modelBuilder.Entity<PricingData>()
                .HasIndex(p => new { p.Provider, p.ExternalItemId })
                .IsUnique()
                .HasDatabaseName("UX_PricingData_Provider_ExternalItemId");

            modelBuilder.Entity<PricingData>()
                .ToTable("PricingData", table => table.HasCheckConstraint(
                    "CK_PricingData_Category", "\"Category\" IN ('material', 'labour')"));

            modelBuilder.Entity<PricingImportAudit>()
                .HasIndex(audit => audit.StartedAt)
                .HasDatabaseName("IX_PricingImportAudits_StartedAt");

            modelBuilder.Entity<HouseDesign>(entity =>
            {
                entity.HasIndex(e => e.WorkflowStateId).HasDatabaseName("IX_HouseDesigns_WorkflowStateId");
                entity.HasIndex(e => new { e.WorkflowStateId, e.IsCurrent }).HasDatabaseName("IX_HouseDesigns_WorkflowState_IsCurrent");
                entity.HasIndex(e => new { e.WorkflowStateId, e.IsArchived }).HasDatabaseName("IX_HouseDesigns_WorkflowState_IsArchived");
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

            modelBuilder.Entity<WorkflowState>()
                .HasIndex(e => e.PreferredHouseDesignId)
                .HasDatabaseName("IX_WorkflowStates_PreferredHouseDesignId");
            modelBuilder.Entity<WorkflowState>()
                .HasOne<HouseDesign>()
                .WithMany()
                .HasForeignKey(e => e.PreferredHouseDesignId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ValidationRequest>()
                .HasOne(x => x.HouseDesign).WithMany().HasForeignKey(x => x.HouseDesignId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ValidationRequest>()
                .HasIndex(x => new { x.WorkflowStateId, x.HouseDesignId, x.Status })
                .HasDatabaseName("IX_ValidationRequests_Workflow_Design_Status");

            modelBuilder.Entity<Project>()
                .HasOne(x => x.HouseDesign).WithMany().HasForeignKey(x => x.HouseDesignId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ConstructorProjectRequest>(entity =>
            {
                entity.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Constructor).WithMany().HasForeignKey(x => x.ConstructorId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.HouseDesign).WithMany().HasForeignKey(x => x.HouseDesignId).OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(x => new { x.HouseDesignId, x.ConstructorId, x.Status })
                    .IsUnique().HasFilter("\"Status\" = 'Pending'");
                entity.HasIndex(x => new { x.ConstructorId, x.Status });
                entity.HasIndex(x => new { x.ProjectId, x.Status })
                    .IsUnique().HasFilter("\"Status\" = 'Accepted'");
            });

            modelBuilder.Entity<Room>(entity =>
            {
                entity.HasIndex(e => e.HouseDesignId).HasDatabaseName("IX_Rooms_HouseDesignId");
            });

            modelBuilder.Entity<CostEstimate>(entity =>
            {
                entity.HasIndex(e => e.HouseDesignId)
                    .IsUnique()
                    .HasDatabaseName("IX_CostEstimates_HouseDesignId");
                entity.HasOne(e => e.HouseDesign)
                    .WithMany(d => d.CostEstimates)
                    .HasForeignKey(e => e.HouseDesignId);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
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
