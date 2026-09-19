using System.Text.Json;

namespace HousePlanner.API.Services;

public static class LayoutRoomCounts
{
    public static int Bathrooms(JsonElement layout)
    {
        if (layout.ValueKind != JsonValueKind.Object ||
            !layout.TryGetProperty("rooms", out var rooms) || rooms.ValueKind != JsonValueKind.Array)
            return 0;

        return rooms.EnumerateArray().Count(room =>
            room.ValueKind == JsonValueKind.Object &&
            room.TryGetProperty("room_type", out var type) && type.ValueKind == JsonValueKind.String &&
            (type.GetString() == "bathroom" || type.GetString()!.StartsWith("bathroom_", StringComparison.Ordinal)));
    }

    public static bool HasMasterEnsuite(JsonElement layout)
    {
        if (layout.ValueKind != JsonValueKind.Object ||
            !layout.TryGetProperty("rooms", out var rooms) || rooms.ValueKind != JsonValueKind.Array)
            return false;

        var ensuiteIds = rooms.EnumerateArray().Where(room => RoomType(room) == "bathroom_attached")
            .Select(RoomId).Where(id => id != null).Cast<string>().ToHashSet();
        if (ensuiteIds.Count == 0 || !layout.TryGetProperty("connections", out var connections) || connections.ValueKind != JsonValueKind.Array)
            return false;
        return ensuiteIds.Any(id => Neighbours(connections, id).SetEquals(["bedroom_1"]));
    }

    public static bool HasSeparateDining(JsonElement layout)
    {
        if (layout.ValueKind != JsonValueKind.Object ||
            !layout.TryGetProperty("rooms", out var rooms) || rooms.ValueKind != JsonValueKind.Array)
            return false;

        return rooms.EnumerateArray().Any(room =>
            room.ValueKind == JsonValueKind.Object &&
            room.TryGetProperty("room_type", out var type) && type.ValueKind == JsonValueKind.String &&
            type.GetString() == "dining");
    }

    public static bool HasOpenPlan(JsonElement layout)
    {
        if (layout.ValueKind != JsonValueKind.Object ||
            !layout.TryGetProperty("connections", out var connections) || connections.ValueKind != JsonValueKind.Array)
            return false;

        return connections.EnumerateArray().Any(conn =>
            conn.ValueKind == JsonValueKind.Object &&
            conn.TryGetProperty("kind", out var kind) && kind.ValueKind == JsonValueKind.String &&
            kind.GetString() == "open");
    }

    public static bool HasBalcony(JsonElement layout)
    {
        return HasConnectedRoom(layout, "balcony", floor => floor > 1);
    }

    public static bool HasVeranda(JsonElement layout)
    {
        return HasConnectedRoom(layout, "veranda", floor => floor == 1);
    }

    public static bool HasOffice(JsonElement layout)
    {
        return HasRoomType(layout, "home_office");
    }

    public static bool HasUtilityRoom(JsonElement layout)
    {
        if (!TryRoomsAndConnections(layout, out var rooms, out var connections)) return false;
        var kitchenIds = rooms.EnumerateArray().Where(r => RoomType(r) == "kitchen").Select(RoomId).Where(x => x != null).Cast<string>().ToHashSet();
        return rooms.EnumerateArray().Where(r => RoomType(r) is "utility" or "utility_room")
            .Select(RoomId).Where(x => x != null).Cast<string>()
            .Any(id => Neighbours(connections, id).Overlaps(kitchenIds));
    }

    private static bool HasRoomType(JsonElement layout, string expectedType)
    {
        if (layout.ValueKind != JsonValueKind.Object ||
            !layout.TryGetProperty("rooms", out var rooms) || rooms.ValueKind != JsonValueKind.Array)
            return false;

        return rooms.EnumerateArray().Any(room =>
            room.ValueKind == JsonValueKind.Object &&
            room.TryGetProperty("room_type", out var type) && type.ValueKind == JsonValueKind.String &&
            type.GetString() == expectedType);
    }

    public static bool HasParking(JsonElement layout)
    {
        if (layout.ValueKind != JsonValueKind.Object) return false;
        if (layout.TryGetProperty("site_features", out var siteFeatures) && siteFeatures.ValueKind == JsonValueKind.Array)
        {
            return siteFeatures.EnumerateArray().Any(feature =>
                feature.ValueKind == JsonValueKind.Object &&
                feature.TryGetProperty("type", out var type) && type.GetString() == "parking");
        }
        return false;
    }

    public static bool IsAccessibleFriendly(JsonElement layout)
    {
        if (layout.ValueKind != JsonValueKind.Object ||
            !layout.TryGetProperty("rooms", out var rooms) || rooms.ValueKind != JsonValueKind.Array)
            return false;

        bool hasBedroom = false;
        bool hasBathroom = false;
        bool usableHalls = true;

        string[] circulationTypes = { "hallway", "corridor", "stairs", "landing" };

        foreach (var room in rooms.EnumerateArray())
        {
            if (room.ValueKind != JsonValueKind.Object) continue;
            
            int floor = 0;
            if (room.TryGetProperty("floor", out var fProp) && fProp.ValueKind == JsonValueKind.Number)
                floor = fProp.GetInt32();
            else if (room.TryGetProperty("floor_number", out var fnProp) && fnProp.ValueKind == JsonValueKind.Number)
                floor = fnProp.GetInt32();

            if (floor != 1) continue;

            string roomType = room.TryGetProperty("room_type", out var tProp) ? tProp.GetString() ?? "" : "";
            
            if (roomType.Contains("bedroom")) hasBedroom = true;
            if (roomType.Contains("bathroom")) hasBathroom = true;

            if (Array.Exists(circulationTypes, t => t == roomType))
            {
                decimal width = room.TryGetProperty("width", out var wProp) ? wProp.GetDecimal() : 0;
                decimal length = room.TryGetProperty("length", out var lProp) ? lProp.GetDecimal() : 0;
                if (Math.Min(width, length) < 3.5m)
                {
                    usableHalls = false;
                }
            }
        }

        return hasBedroom && hasBathroom && usableHalls;
    }

    private static bool HasConnectedRoom(JsonElement layout, string type, Func<int, bool> floorRule)
    {
        if (!TryRoomsAndConnections(layout, out var rooms, out var connections)) return false;
        return rooms.EnumerateArray().Where(r => RoomType(r) == type && floorRule(RoomFloor(r)))
            .Select(RoomId).Where(x => x != null).Cast<string>()
            .Any(id => Neighbours(connections, id).Count > 0);
    }

    private static bool TryRoomsAndConnections(JsonElement layout, out JsonElement rooms, out JsonElement connections)
    {
        rooms = default;
        connections = default;
        return layout.ValueKind == JsonValueKind.Object &&
               layout.TryGetProperty("rooms", out rooms) && rooms.ValueKind == JsonValueKind.Array &&
               layout.TryGetProperty("connections", out connections) && connections.ValueKind == JsonValueKind.Array;
    }

    private static string? RoomId(JsonElement room) =>
        room.TryGetProperty("room_id", out var id) && id.ValueKind == JsonValueKind.String ? id.GetString() : null;

    private static string RoomType(JsonElement room) =>
        room.TryGetProperty("room_type", out var type) && type.ValueKind == JsonValueKind.String ? type.GetString() ?? "" : "";

    private static int RoomFloor(JsonElement room) =>
        room.TryGetProperty("floor", out var floor) && floor.ValueKind == JsonValueKind.Number ? floor.GetInt32() : 0;

    private static HashSet<string> Neighbours(JsonElement connections, string roomId)
    {
        var result = new HashSet<string>();
        foreach (var connection in connections.EnumerateArray())
        {
            var from = connection.TryGetProperty("from_room", out var f) ? f.GetString() : null;
            var to = connection.TryGetProperty("to_room", out var t) ? t.GetString() : null;
            if (from == roomId && to != null) result.Add(to);
            if (to == roomId && from != null) result.Add(from);
        }
        return result;
    }
}
