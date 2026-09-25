using System.Text.Json;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;

namespace HousePlanner.API.Tests.Services;

public class CostBreakdownBuilderTests
{
    [Fact]
    public void Build_UsesPersistedSnapshotAndReconcilesToSavedTotals()
    {
        var estimate = new CostEstimate
        {
            MaterialCostLkr = 17_262_000m,
            LabourCostLkr = 6_041_700m,
            TotalCostLkr = 23_303_700m,
            PricingSnapshotJson = JsonSerializer.Serialize(new object[]
            {
                Material("Foundation Materials", "Foundation", 3_000m),
                Material("Structural Materials", "Structural", 4_500m),
                Material("Roofing Materials", "Roofing", 2_500m),
                Material("Finishing Materials", "Finishing", 2_500m),
                Material("MEP Materials", "MEP", 1_200m),
                new
                {
                    itemName = "Construction Labour",
                    displayGroup = "Labour",
                    category = "labour",
                    unitCostLkr = 0.35m,
                    unit = "factor",
                    terrainMultiplier = new { flat = 1m, hillside = 1.15m, coastal = 1.10m }
                }
            })
        };

        var breakdown = CostBreakdownBuilder.Build(estimate, "flat");

        Assert.Equal(6, breakdown.Count);
        Assert.Equal(3_780_000m, breakdown.Single(item => item.ItemName == "Foundation Materials").AmountLkr);
        Assert.Equal(5_670_000m, breakdown.Single(item => item.ItemName == "Structural Materials").AmountLkr);
        Assert.Equal(1_512_000m, breakdown.Single(item => item.ItemName == "MEP Materials").AmountLkr);
        Assert.Equal(6_041_700m, breakdown.Single(item => item.Category == "labour").AmountLkr);
        Assert.All(breakdown.Where(item => item.Category == "material"), item => Assert.Equal(1_260m, item.AppliedQuantity));
        Assert.Equal(estimate.TotalCostLkr, breakdown.Sum(item => item.AmountLkr));
        Assert.Equal(100m, breakdown.Sum(item => item.SharePercent));
    }

    [Fact]
    public void Build_UsesTerrainMultiplierFromTheSavedSnapshot()
    {
        var estimate = new CostEstimate
        {
            MaterialCostLkr = 3_450m,
            LabourCostLkr = 0m,
            TotalCostLkr = 3_450m,
            PricingSnapshotJson = JsonSerializer.Serialize(new[] { Material("Foundation Materials", "Foundation", 3_000m) })
        };

        var item = Assert.Single(CostBreakdownBuilder.Build(estimate, "hillside"));

        Assert.Equal(1.15m, item.TerrainMultiplier);
        Assert.Equal(1m, item.AppliedQuantity);
        Assert.Equal(3_450m, item.AmountLkr);
    }

    [Fact]
    public void Build_InvalidSnapshotReturnsNoInventedMaterialRows()
    {
        var estimate = new CostEstimate
        {
            MaterialCostLkr = 100m,
            LabourCostLkr = 35m,
            TotalCostLkr = 135m,
            PricingSnapshotJson = "not-json"
        };

        var breakdown = CostBreakdownBuilder.Build(estimate, "flat");

        var labour = Assert.Single(breakdown);
        Assert.Equal("Construction Labour", labour.ItemName);
        Assert.Equal(35m, labour.AmountLkr);
    }

    private static object Material(string name, string group, decimal rate) => new
    {
        itemName = name,
        displayGroup = group,
        category = "material",
        unitCostLkr = rate,
        unit = "per_sqft",
        terrainMultiplier = new { flat = 1m, hillside = 1.15m, coastal = 1.10m }
    };
}
