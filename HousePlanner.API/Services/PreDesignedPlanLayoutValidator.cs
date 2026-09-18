using System.Linq;
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
            try
            {
                rooms.Add(new Rect(
                    room.GetProperty("room_id").GetString() ?? "",
                    room.GetProperty("room_type").GetString() ?? "",
                    room.GetProperty("floor").GetInt32(),
                    room.GetProperty("x").GetDecimal(),
                    room.GetProperty("y").GetDecimal(),
                    room.GetProperty("width").GetDecimal(),
                    room.GetProperty("length").GetDecimal()));
            }
            catch
            {
                errors.Add("Every room requires room_id, room_type, floor, x, y, width and length.");
            }
        }

        if (rooms.Any(r => r.W <= 0 || r.L <= 0 || r.X < 0 || r.Y < 0)) errors.Add("Room coordinates and dimensions must be valid positive values.");
        if (rooms.Select(r => r.Id).Distinct().Count() != rooms.Count) errors.Add("Room IDs must be unique.");
        if (rooms.Count(r => r.Type.Contains("bedroom", StringComparison.OrdinalIgnoreCase)) != bedrooms) errors.Add("Layout bedroom count does not match metadata.");
        if (rooms.Select(r => r.Floor).Distinct().Count() != floors) errors.Add("Layout floor count does not match metadata.");

        for (var i = 0; i < rooms.Count; i++)
        {
            for (var j = i + 1; j < rooms.Count; j++)
            {
                var a = rooms[i];
                var b = rooms[j];
                if (a.Floor != b.Floor) continue;
                if (Math.Min(a.X + a.W, b.X + b.W) - Math.Max(a.X, b.X) > 0 &&
                    Math.Min(a.Y + a.L, b.Y + b.L) - Math.Max(a.Y, b.Y) > 0)
                {
                    errors.Add($"Rooms {a.Id} and {b.Id} overlap.");
                }
            }
        }

        if (!rooms.Any(r => r.Type.Contains("living", StringComparison.OrdinalIgnoreCase))) errors.Add("Layout requires a living room.");
        if (!rooms.Any(r => r.Type.Contains("kitchen", StringComparison.OrdinalIgnoreCase))) errors.Add("Layout requires a kitchen.");
        if (!rooms.Any(r => r.Type.Contains("bath", StringComparison.OrdinalIgnoreCase))) errors.Add("Layout requires a bathroom.");

        foreach (var room in rooms)
        {
            var minimum = room.Type.Contains("bedroom", StringComparison.OrdinalIgnoreCase) ? (9m, 10m) :
                room.Type.Contains("bath", StringComparison.OrdinalIgnoreCase) ? (5m, 5m) :
                room.Type.Contains("kitchen", StringComparison.OrdinalIgnoreCase) ? (8m, 8m) :
                room.Type.Contains("living", StringComparison.OrdinalIgnoreCase) ? (10m, 10m) :
                room.Type.Contains("hall", StringComparison.OrdinalIgnoreCase) ? (3.5m, 4m) : (0m, 0m);
            if (minimum.Item1 > 0 && !((room.W >= minimum.Item1 && room.L >= minimum.Item2) || (room.W >= minimum.Item2 && room.L >= minimum.Item1)))
                errors.Add($"Room {room.Id} is below conceptual minimum dimensions for {room.Type}.");
        }

        var graph = rooms.ToDictionary(r => r.Id, _ => new HashSet<string>());
        if (!layout.TryGetProperty("connections", out var connections) || connections.ValueKind != JsonValueKind.Array)
            errors.Add("Layout must contain actual room connections.");
        else
        {
            foreach (var connection in connections.EnumerateArray())
            {
                var from = connection.TryGetProperty("from_room", out var f) ? f.GetString() : null;
                var to = connection.TryGetProperty("to_room", out var t) ? t.GetString() : null;
                if (from is null || to is null || !graph.ContainsKey(from) || !graph.ContainsKey(to))
                {
                    errors.Add("Every connection must reference existing room IDs.");
                    continue;
                }

                var a = rooms.First(r => r.Id == from);
                var b = rooms.First(r => r.Id == to);
                var stairLink = a.Type.StartsWith("staircase", StringComparison.OrdinalIgnoreCase) && b.Type.StartsWith("staircase", StringComparison.OrdinalIgnoreCase);
                if (a.Floor != b.Floor && !stairLink) errors.Add($"Connection {from} to {to} crosses floors without a staircase.");
                if (a.Floor == b.Floor && SharedWall(a, b) < 2.5m) errors.Add($"Connected rooms {from} and {to} do not share enough wall for a door.");
                graph[from].Add(to);
                graph[to].Add(from);
            }
        }

        var internalArea = rooms.Sum(r => r.W * r.L);
        var circulationRooms = rooms.Where(r => r.Type.Contains("hall", StringComparison.OrdinalIgnoreCase) || r.Type.Contains("corridor", StringComparison.OrdinalIgnoreCase) || r.Type.Contains("foyer", StringComparison.OrdinalIgnoreCase) || r.Type.Contains("entrance", StringComparison.OrdinalIgnoreCase) || r.Type.Contains("stair", StringComparison.OrdinalIgnoreCase)).ToList();
        var circulationArea = circulationRooms.Sum(r => r.W * r.L);
        if (internalArea > 0 && circulationArea / internalArea > 0.15m) errors.Add("Layout has excessive circulation area.");

        foreach (var hallway in circulationRooms.Where(r => r.Type.Contains("hall", StringComparison.OrdinalIgnoreCase)))
        {
            var width = Math.Min(hallway.W, hallway.L);
            var length = Math.Max(hallway.W, hallway.L);
            var aspect = length / Math.Max(width, 0.01m);
            var floorRooms = rooms.Where(r => r.Floor == hallway.Floor).ToList();
            var span = Math.Max(
                floorRooms.Max(r => r.X + r.W) - floorRooms.Min(r => r.X),
                floorRooms.Max(r => r.Y + r.L) - floorRooms.Min(r => r.Y));
            var dependentRooms = graph.TryGetValue(hallway.Id, out var deps) ? deps.Count : 0;
            if (length > 28m || aspect > 6.5m || length / Math.Max(span, 0.01m) > 0.75m || (dependentRooms >= 4 && length > 20m))
                errors.Add("Layout has an unrealistic hallway spine.");
        }

        var bedroomsByFloor = rooms.Where(r => r.Type.Contains("bedroom", StringComparison.OrdinalIgnoreCase)).GroupBy(r => r.Floor);
        foreach (var group in bedroomsByFloor)
        {
            var list = group.ToList();
            for (var i = 0; i < list.Count; i++)
            {
                for (var j = i + 1; j < list.Count; j++)
                {
                    if (Distance(list[i], list[j]) > 44m)
                    {
                        errors.Add("Bedrooms are too spread out for a coherent private zone.");
                        i = list.Count;
                        break;
                    }
                }
            }
        }

        var living = rooms.Where(r => r.Type.Contains("living", StringComparison.OrdinalIgnoreCase)).ToList();
        var kitchen = rooms.Where(r => r.Type.Contains("kitchen", StringComparison.OrdinalIgnoreCase)).ToList();
        var dining = rooms.Where(r => r.Type.Contains("dining", StringComparison.OrdinalIgnoreCase)).ToList();
        var publicRooms = living.Concat(kitchen).Concat(dining).ToList();
        if (living.Any() && kitchen.Any() && MinDistance(living, kitchen) > 30m) errors.Add("Living and kitchen are too far apart.");
        if (dining.Any() && MinDistance(dining, publicRooms) > 30m) errors.Add("Dining is too far from the public zone.");

        var bedroomRooms = rooms.Where(r => r.Type.Contains("bedroom", StringComparison.OrdinalIgnoreCase)).ToList();
        var commonBaths = rooms.Where(r => r.Type.Contains("bath", StringComparison.OrdinalIgnoreCase) && !r.Type.Contains("attached", StringComparison.OrdinalIgnoreCase)).ToList();
        if (bedroomRooms.Any() && commonBaths.Any() && MinDistance(commonBaths, bedroomRooms) > 32m) errors.Add("Common bathrooms are too far from the bedroom cluster.");

        if (layout.TryGetProperty("entrances", out var entrances) && entrances.ValueKind == JsonValueKind.Array && entrances.GetArrayLength() > 0)
        {
            var entranceIds = entrances.EnumerateArray().Select(e => e.TryGetProperty("room_id", out var id) ? id.GetString() : null).Where(id => id != null && graph.ContainsKey(id)).Cast<string>().ToList();
            if (entranceIds.Count == 0) errors.Add("Entrance must reference an existing room.");
            else
            {
                var entranceRoom = rooms.First(r => r.Id == entranceIds[0]);
                if (!entranceRoom.Type.Contains("living", StringComparison.OrdinalIgnoreCase) && !entranceRoom.Type.Contains("foyer", StringComparison.OrdinalIgnoreCase) && !entranceRoom.Type.Contains("entrance", StringComparison.OrdinalIgnoreCase) && !entranceRoom.Type.Contains("dining", StringComparison.OrdinalIgnoreCase))
                    errors.Add("Entrance must lead into a public zone or foyer.");
                if (publicRooms.Any() && MinDistance(new[] { entranceRoom }, publicRooms) > 14m)
                    errors.Add("Entrance is too far from the public zone.");
                var visited = new HashSet<string>(entranceIds);
                var queue = new Queue<string>(entranceIds);
                while (queue.TryDequeue(out var id))
                    foreach (var next in graph[id])
                        if (visited.Add(next)) queue.Enqueue(next);
                if (visited.Count != rooms.Count) errors.Add("Every room must be reachable from the entrance through actual connections.");
            }
        }
        else
        {
            errors.Add("Layout requires an exterior entrance.");
        }

        if (layout.TryGetProperty("template_id", out var templateIdProp))
        {
            var templateId = templateIdProp.GetString() ?? string.Empty;
            var bboxWidth = rooms.Max(r => r.X + r.W) - rooms.Min(r => r.X);
            var bboxLength = rooms.Max(r => r.Y + r.L) - rooms.Min(r => r.Y);
            var aspect = Math.Max(bboxWidth, bboxLength) / Math.Max(Math.Min(bboxWidth, bboxLength), 0.01m);
            var density = internalArea / Math.Max(bboxWidth * bboxLength, 0.01m);
            if (templateId == "COMPACT_RECTANGLE" && (aspect > 1.8m || density < 0.90m)) errors.Add("Compact rectangle topology does not match the geometry.");
            if (templateId == "LINEAR" && (aspect <= 1.8m || aspect > 2.8m)) errors.Add("Linear topology does not match the geometry.");
            if (templateId == "L_SHAPE" && density < 0.60m) errors.Add("L-shape topology is too loose to be plausible.");
            if (templateId == "T_SHAPE" && !rooms.Any(r => r.Type.Contains("hall", StringComparison.OrdinalIgnoreCase))) errors.Add("T-shape topology requires meaningful circulation.");
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

    private static decimal Distance(Rect a, Rect b) =>
        Math.Abs((a.X + a.W / 2) - (b.X + b.W / 2)) + Math.Abs((a.Y + a.L / 2) - (b.Y + b.L / 2));

    private static decimal MinDistance(IEnumerable<Rect> left, IEnumerable<Rect> right) =>
        left.SelectMany(a => right.Select(b => Distance(a, b))).DefaultIfEmpty(decimal.MaxValue).Min();
}
