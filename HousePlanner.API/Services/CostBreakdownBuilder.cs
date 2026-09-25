using System.Text.Json;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;

namespace HousePlanner.API.Services;

/// <summary>
/// Reconstructs the auditable cost-head breakdown from the immutable pricing
/// snapshot stored with an estimate. It never reads today's pricing catalogue.
/// </summary>
public static class CostBreakdownBuilder
{
    public static CostSummaryDto ToSummary(
        decimal materialCostLkr,
        decimal labourCostLkr,
        decimal totalCostLkr,
        decimal? budgetDeltaPercent,
        string pricingSnapshotJson,
        string? terrainType = null) => ToSummary(new CostEstimate
        {
            MaterialCostLkr = materialCostLkr,
            LabourCostLkr = labourCostLkr,
            TotalCostLkr = totalCostLkr,
            BudgetDeltaPercent = budgetDeltaPercent,
            PricingSnapshotJson = pricingSnapshotJson
        }, terrainType);

    public static CostSummaryDto ToSummary(CostEstimate estimate, string? terrainType = null) => new(
        estimate.MaterialCostLkr,
        estimate.LabourCostLkr,
        estimate.TotalCostLkr,
        estimate.BudgetDeltaPercent,
        Build(estimate, terrainType),
        estimate.FormulaVersion,
        estimate.AppliedAreaSqft > 0 ? estimate.AppliedAreaSqft : null,
        estimate.TerrainType,
        estimate.CreatedAt);

    public static IReadOnlyList<CostBreakdownItemDto> Build(
        CostEstimate estimate,
        string? terrainType = null)
    {
        var persisted = ParseBreakdown(estimate.BreakdownJson);
        if (persisted.Count > 0)
            return ReconcileShares(persisted, estimate.TotalCostLkr);

        var records = ParseSnapshot(estimate.PricingSnapshotJson);
        var terrain = NormalizeTerrain(terrainType ?? estimate.HouseDesign?.TerrainType);
        var materials = records
            .Where(record => record.Category.Equals("material", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var effectiveRateTotal = materials.Sum(record =>
            record.UnitCostLkr * GetTerrainMultiplier(record.TerrainMultipliers, terrain));

        var appliedArea = effectiveRateTotal > 0m
            ? estimate.MaterialCostLkr / effectiveRateTotal
            : 0m;

        var result = new List<CostBreakdownItemDto>();
        decimal allocatedMaterial = 0m;
        for (var index = 0; index < materials.Count; index++)
        {
            var record = materials[index];
            var multiplier = GetTerrainMultiplier(record.TerrainMultipliers, terrain);
            var amount = index == materials.Count - 1
                ? estimate.MaterialCostLkr - allocatedMaterial
                : decimal.Round(appliedArea * record.UnitCostLkr * multiplier, 2);
            allocatedMaterial += amount;

            result.Add(ToItem(
                record,
                appliedArea,
                "sq ft",
                multiplier,
                amount,
                estimate.TotalCostLkr));
        }

        var labour = records.FirstOrDefault(record =>
            record.Category.Equals("labour", StringComparison.OrdinalIgnoreCase));
        if (labour is not null)
        {
            result.Add(ToItem(
                labour,
                estimate.MaterialCostLkr,
                "material cost",
                1m,
                estimate.LabourCostLkr,
                estimate.TotalCostLkr));
        }
        else if (estimate.LabourCostLkr > 0m)
        {
            result.Add(new CostBreakdownItemDto(
                "Construction Labour",
                "Labour",
                "labour",
                estimate.MaterialCostLkr > 0m ? estimate.LabourCostLkr / estimate.MaterialCostLkr : 0m,
                "factor",
                estimate.MaterialCostLkr,
                "material cost",
                1m,
                estimate.LabourCostLkr,
                Share(estimate.LabourCostLkr, estimate.TotalCostLkr)));
        }

        return ReconcileShares(result, estimate.TotalCostLkr);
    }

    private static CostBreakdownItemDto ToItem(
        SnapshotRecord record,
        decimal appliedQuantity,
        string quantityUnit,
        decimal terrainMultiplier,
        decimal amount,
        decimal total) => new(
            record.ItemName,
            string.IsNullOrWhiteSpace(record.DisplayGroup)
                ? record.ItemName.Replace(" Materials", string.Empty, StringComparison.OrdinalIgnoreCase)
                : record.DisplayGroup,
            record.Category.ToLowerInvariant(),
            record.UnitCostLkr,
            record.Unit,
            decimal.Round(appliedQuantity, 2),
            quantityUnit,
            terrainMultiplier,
            amount,
            Share(amount, total),
            record.Provider,
            record.SourceReference,
            record.PricingUpdatedAt);

    private static IReadOnlyList<CostBreakdownItemDto> ReconcileShares(
        List<CostBreakdownItemDto> items,
        decimal total)
    {
        if (items.Count > 0 && Math.Abs(items.Sum(item => item.AmountLkr) - total) <= 0.05m)
        {
            var displayedShareTotal = items.Sum(item => item.SharePercent);
            items[^1] = items[^1] with
            {
                SharePercent = items[^1].SharePercent + (100m - displayedShareTotal)
            };
        }
        return items;
    }

    private static List<CostBreakdownItemDto> ParseBreakdown(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]") return [];
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array) return [];
            var result = new List<CostBreakdownItemDto>();
            foreach (var element in document.RootElement.EnumerateArray())
            {
                var itemName = ReadString(element, "itemName", "item_name");
                var category = ReadString(element, "category").ToLowerInvariant();
                var amount = ReadDecimal(element, "amountLkr", "amount_lkr");
                if (string.IsNullOrWhiteSpace(itemName) || amount is null ||
                    category is not ("material" or "labour")) continue;
                result.Add(new CostBreakdownItemDto(
                    itemName,
                    ReadString(element, "costHead", "cost_head") is { Length: > 0 } head ? head : itemName,
                    category,
                    ReadDecimal(element, "unitCostLkr", "unit_cost_lkr") ?? 0m,
                    ReadString(element, "unit"),
                    ReadDecimal(element, "appliedQuantity", "applied_quantity") ?? 0m,
                    ReadString(element, "quantityUnit", "quantity_unit"),
                    ReadDecimal(element, "terrainMultiplier", "terrain_multiplier") ?? 1m,
                    amount.Value,
                    ReadDecimal(element, "sharePercent", "share_percent") ?? 0m,
                    ReadString(element, "provider"),
                    ReadString(element, "sourceReference", "source_reference"),
                    ReadDate(element, "pricingUpdatedAt", "pricing_updated_at", "updatedAt", "updated_at")));
            }
            return result;
        }
        catch (JsonException) { return []; }
    }

    private static decimal Share(decimal amount, decimal total) =>
        total > 0m ? decimal.Round(amount / total * 100m, 2) : 0m;

    private static string NormalizeTerrain(string? terrain) =>
        terrain?.Trim().ToLowerInvariant() switch
        {
            "hillside" => "hillside",
            "coastal" => "coastal",
            _ => "flat"
        };

    private static decimal GetTerrainMultiplier(
        IReadOnlyDictionary<string, decimal> multipliers,
        string terrain) =>
        multipliers.TryGetValue(terrain, out var value) && value > 0m ? value : 1m;

    private static List<SnapshotRecord> ParseSnapshot(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array) return [];

            var records = new List<SnapshotRecord>();
            foreach (var element in document.RootElement.EnumerateArray())
            {
                var itemName = ReadString(element, "itemName", "item_name");
                var category = ReadString(element, "category");
                var unit = ReadString(element, "unit");
                var unitCost = ReadDecimal(element, "unitCostLkr", "unit_cost_lkr");
                if (string.IsNullOrWhiteSpace(itemName) || string.IsNullOrWhiteSpace(category) ||
                    string.IsNullOrWhiteSpace(unit) || unitCost is null || unitCost < 0m)
                    continue;

                var multipliers = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                if (TryGetProperty(element, out var multiplierElement, "terrainMultiplier", "terrain_multiplier") &&
                    multiplierElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in multiplierElement.EnumerateObject())
                        if (property.Value.TryGetDecimal(out var multiplier))
                            multipliers[property.Name] = multiplier;
                }

                records.Add(new SnapshotRecord(
                    itemName,
                    ReadString(element, "displayGroup", "display_group"),
                    category,
                    unitCost.Value,
                    unit,
                    multipliers,
                    ReadString(element, "provider"),
                    ReadString(element, "sourceReference", "source_reference"),
                    ReadDate(element, "updatedAt", "updated_at")));
            }

            return records;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string ReadString(JsonElement element, params string[] names) =>
        TryGetProperty(element, out var value, names) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static decimal? ReadDecimal(JsonElement element, params string[] names) =>
        TryGetProperty(element, out var value, names) && value.TryGetDecimal(out var number)
            ? number
            : null;

    private static DateTimeOffset? ReadDate(JsonElement element, params string[] names) =>
        TryGetProperty(element, out var value, names) && value.ValueKind == JsonValueKind.String &&
        DateTimeOffset.TryParse(value.GetString(), out var date) ? date : null;

    private static bool TryGetProperty(JsonElement element, out JsonElement value, params string[] names)
    {
        foreach (var property in element.EnumerateObject())
            foreach (var name in names)
                if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }

        value = default;
        return false;
    }

    private sealed record SnapshotRecord(
        string ItemName,
        string DisplayGroup,
        string Category,
        decimal UnitCostLkr,
        string Unit,
        IReadOnlyDictionary<string, decimal> TerrainMultipliers,
        string Provider,
        string SourceReference,
        DateTimeOffset? PricingUpdatedAt);
}
