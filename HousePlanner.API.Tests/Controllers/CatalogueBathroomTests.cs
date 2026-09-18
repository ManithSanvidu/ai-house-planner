using System.Text.Json;
using HousePlanner.API.Data;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace HousePlanner.API.Tests.Controllers;

public class CatalogueBathroomTests
{
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public async Task Seeder_OnlyAcceptsExactBathroomMetadata(int declared, bool accepted)
    {
        var root = Path.Combine(Path.GetTempPath(), "houseplanner-bathrooms-" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(root, "Data", "Seed"));
        try
        {
            var path = Path.Combine(root, "Data", "Seed", "pre-designed-plans.json");
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new[] { new
            {
                Name = "Existing test plan", Slug = "test", DesignCode = "TEST", Style = "modern",
                Bedrooms = 3, Bathrooms = declared, FloorCount = 2, TotalBuiltUpAreaSqft = 1810,
                MinimumLandSizePerches = 20, SuitableTerrain = "flat", Tags = Array.Empty<string>(),
                IsActive = true, Layout = new { rooms = new[] {
                    new { room_type = "bathroom_1" }, new { room_type = "bathroom_attached" }
                } }
            } }));
            await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(e => e.ContentRootPath).Returns(root);
            var validator = new Mock<IPreDesignedPlanLayoutValidator>();
            validator.Setup(v => v.Validate(It.IsAny<JsonElement>(), 3, 2)).Returns(Array.Empty<string>());
            var seeder = new PreDesignedPlanSeeder(db, validator.Object, environment.Object,
                Mock.Of<ILogger<PreDesignedPlanSeeder>>());

            await seeder.SeedAsync();

            Assert.Equal(accepted ? 1 : 0, await db.PreDesignedHousePlans.CountAsync());
            if (accepted)
                Assert.Equal(2, (await db.PreDesignedHousePlans.SingleAsync()).Bathrooms);
            else
                validator.Verify(v => v.Validate(It.IsAny<JsonElement>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
