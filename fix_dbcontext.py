import sys

filepath = "HousePlanner.API/Data/ApplicationDbContext.cs"
with open(filepath, 'r') as f:
    content = f.read()

target1 = "        public DbSet<ConstructorProjectRequest> ConstructorProjectRequests { get; set; }"
replacement1 = """        public DbSet<ConstructorProjectRequest> ConstructorProjectRequests { get; set; }
        public DbSet<DailyConstructionLog> DailyConstructionLogs { get; set; }"""

target2 = "            modelBuilder.Entity<PricingData>()"
replacement2 = """            modelBuilder.Entity<DailyConstructionLog>(entity =>
            {
                entity.HasIndex(e => e.ProjectId);
                entity.HasIndex(e => e.ConstructorId);
                entity.HasIndex(e => e.LogDate);
            });

            modelBuilder.Entity<PricingData>()"""

if target1 in content and target2 in content:
    content = content.replace(target1, replacement1)
    content = content.replace(target2, replacement2)
    with open(filepath, 'w') as f:
        f.write(content)
    print("ApplicationDbContext.cs updated")
else:
    print("Targets not found!")
