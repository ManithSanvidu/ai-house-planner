using System.Text.Json;

namespace HousePlanner.API.Services;

public interface IPreDesignedPlanLayoutValidator { IReadOnlyList<string> Validate(JsonElement layout, int bedrooms, int floors); }

public class PreDesignedPlanLayoutValidator : IPreDesignedPlanLayoutValidator
{
    private record Rect(string Id, string Type, int Floor, decimal X, decimal Y, decimal W, decimal L);
    public IReadOnlyList<string> Validate(JsonElement layout, int bedrooms, int floors)
    {
        var errors = new List<string>();
        if (layout.ValueKind != JsonValueKind.Object) return ["Layout must be a JSON object."];
        if (!layout.TryGetProperty("rooms", out var roomsJson) || roomsJson.ValueKind != JsonValueKind.Array)
            return ["Layout must contain a rooms array."];
        var rooms = new List<Rect>();
        foreach (var room in roomsJson.EnumerateArray())
        {
            try {
                rooms.Add(new(room.GetProperty("room_id").GetString() ?? "", room.GetProperty("room_type").GetString() ?? "",
                    room.GetProperty("floor").GetInt32(), room.GetProperty("x").GetDecimal(), room.GetProperty("y").GetDecimal(),
                    room.GetProperty("width").GetDecimal(), room.GetProperty("length").GetDecimal()));
            } catch { errors.Add("Every room requires room_id, room_type, floor, x, y, width and length."); }
        }
        if (rooms.Any(r => r.W <= 0 || r.L <= 0 || r.X < 0 || r.Y < 0)) errors.Add("Room coordinates and dimensions must be valid positive values.");
        if (rooms.Select(r => r.Id).Distinct().Count() != rooms.Count) errors.Add("Room IDs must be unique.");
        if (rooms.Count(r => r.Type.Contains("bedroom", StringComparison.OrdinalIgnoreCase)) != bedrooms) errors.Add("Layout bedroom count does not match metadata.");
        if (rooms.Select(r => r.Floor).Distinct().Count() != floors) errors.Add("Layout floor count does not match metadata.");
        for (var i = 0; i < rooms.Count; i++) for (var j = i + 1; j < rooms.Count; j++)
        {
            var a = rooms[i]; var b = rooms[j]; if (a.Floor != b.Floor) continue;
            if (Math.Min(a.X + a.W, b.X + b.W) - Math.Max(a.X, b.X) > 0 &&
                Math.Min(a.Y + a.L, b.Y + b.L) - Math.Max(a.Y, b.Y) > 0)
                errors.Add($"Rooms {a.Id} and {b.Id} overlap.");
        }
        if (!rooms.Any(r => r.Type.Contains("living"))) errors.Add("Layout requires a living room.");
        if (!rooms.Any(r => r.Type.Contains("kitchen"))) errors.Add("Layout requires a kitchen.");
        if (!rooms.Any(r => r.Type.Contains("bath"))) errors.Add("Layout requires a bathroom.");
        return errors.Distinct().ToList();
    }
}
