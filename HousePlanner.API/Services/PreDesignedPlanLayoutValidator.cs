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
        foreach (var room in rooms)
        {
            var minimum = room.Type.Contains("bedroom") ? (9m, 10m) : room.Type.Contains("bath") ? (5m, 5m) :
                room.Type.Contains("kitchen") ? (8m, 8m) : room.Type.Contains("living") ? (10m, 10m) :
                room.Type.Contains("hall") ? (3.5m, 4m) : (0m, 0m);
            if (minimum.Item1 > 0 && !((room.W >= minimum.Item1 && room.L >= minimum.Item2) || (room.W >= minimum.Item2 && room.L >= minimum.Item1)))
                errors.Add($"Room {room.Id} is below conceptual minimum dimensions for {room.Type}.");
        }

        var graph = rooms.ToDictionary(r => r.Id, _ => new HashSet<string>());
        if (!layout.TryGetProperty("connections", out var connections) || connections.ValueKind != JsonValueKind.Array)
            errors.Add("Layout must contain actual room connections.");
        else foreach (var connection in connections.EnumerateArray())
        {
            var from = connection.TryGetProperty("from_room", out var f) ? f.GetString() : null;
            var to = connection.TryGetProperty("to_room", out var t) ? t.GetString() : null;
            if (from is null || to is null || !graph.ContainsKey(from) || !graph.ContainsKey(to))
            { errors.Add("Every connection must reference existing room IDs."); continue; }
            var a = rooms.First(r => r.Id == from); var b = rooms.First(r => r.Id == to);
            var stairLink = a.Type.StartsWith("staircase") && b.Type.StartsWith("staircase");
            if (a.Floor != b.Floor && !stairLink) errors.Add($"Connection {from} to {to} crosses floors without a staircase.");
            if (a.Floor == b.Floor && SharedWall(a, b) < 2.5m) errors.Add($"Connected rooms {from} and {to} do not share enough wall for a door.");
            graph[from].Add(to); graph[to].Add(from);
        }

        if (!layout.TryGetProperty("entrances", out var entrances) || entrances.ValueKind != JsonValueKind.Array || entrances.GetArrayLength() == 0)
            errors.Add("Layout requires an exterior entrance.");
        else
        {
            var entranceIds = entrances.EnumerateArray().Select(e => e.TryGetProperty("room_id", out var id) ? id.GetString() : null).Where(id => id != null && graph.ContainsKey(id)).Cast<string>().ToList();
            if (entranceIds.Count == 0) errors.Add("Entrance must reference an existing room.");
            else { var visited = new HashSet<string>(entranceIds); var queue = new Queue<string>(entranceIds); while(queue.TryDequeue(out var id)) foreach(var next in graph[id]) if(visited.Add(next)) queue.Enqueue(next); if(visited.Count != rooms.Count) errors.Add("Every room must be reachable from the entrance through actual connections."); }
        }
        return errors.Distinct().ToList();
    }

    private static decimal SharedWall(Rect a, Rect b)
    {
        const decimal epsilon = 0.02m;
        if (Math.Abs((a.X + a.W) - b.X) <= epsilon || Math.Abs((b.X + b.W) - a.X) <= epsilon)
            return Math.Max(0, Math.Min(a.Y + a.L, b.Y + b.L) - Math.Max(a.Y, b.Y));
        if (Math.Abs((a.Y + a.L) - b.Y) <= epsilon || Math.Abs((b.Y + b.L) - a.Y) <= epsilon)
            return Math.Max(0, Math.Min(a.X + a.W, b.X + b.W) - Math.Max(a.X, b.X));
        return 0;
    }
}
