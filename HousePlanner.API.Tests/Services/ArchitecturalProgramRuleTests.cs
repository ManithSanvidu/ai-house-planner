using System.Text.Json;
using System.Text.Json.Nodes;
using HousePlanner.API.Services;

namespace HousePlanner.API.Tests.Services;

public class ArchitecturalProgramRuleTests
{
    private static JsonObject SeedLayout(Func<JsonObject, bool> predicate)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "pre-designed-plans.json");
        var plans = JsonNode.Parse(File.ReadAllText(path))!.AsArray();
        return plans.Select(node => node!.AsObject()).First(predicate)["layout"]!.AsObject().DeepClone().AsObject();
    }

    private static JsonElement Element(JsonNode node) =>
        JsonDocument.Parse(node.ToJsonString()).RootElement.Clone();

    [Fact]
    public void SingleFloorLayout_WithStair_IsRejected()
    {
        var layout = SeedLayout(plan => plan["floors"]!.GetValue<int>() == 1);
        layout["rooms"]!.AsArray()[0]!["room_type"] = "staircase";

        var errors = new PreDesignedPlanLayoutValidator().Validate(
            Element(layout), layout["rooms"]!.AsArray().Count(r => r!["room_type"]!.GetValue<string>().Contains("bedroom")), 1);

        Assert.Contains(errors, error => error.Contains("must not contain a staircase"));
    }

    [Fact]
    public void MultiFloorLayout_WithoutStair_IsRejected()
    {
        var plan = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "pre-designed-plans.json")))!
            .AsArray().Select(node => node!.AsObject()).First(item => item["floors"]!.GetValue<int>() == 2);
        var layout = plan["layout"]!.AsObject().DeepClone().AsObject();
        foreach (var room in layout["rooms"]!.AsArray().Select(r => r!.AsObject()))
            if (room["room_type"]!.GetValue<string>().Contains("stair")) room["room_type"] = "foyer";

        var errors = new PreDesignedPlanLayoutValidator().Validate(Element(layout), plan["bedrooms"]!.GetValue<int>(), 2);

        Assert.Contains(errors, error => error.Contains("require a staircase"));
    }

    [Fact]
    public void CapabilityChecks_RequireRealGeometryAndConnections()
    {
        var fake = Element(JsonNode.Parse("""
        {"rooms":[
          {"room_id":"bedroom_1","room_type":"bedroom_1","floor":1,"x":0,"y":0,"width":10,"length":10},
          {"room_id":"bath","room_type":"bathroom_attached","floor":1,"x":10,"y":0,"width":5,"length":5}
        ],"connections":[],"site_features":[{"type":"garden"}]}
        """)!);

        Assert.False(LayoutRoomCounts.HasMasterEnsuite(fake));
        Assert.False(LayoutRoomCounts.HasParking(fake));
    }
}
