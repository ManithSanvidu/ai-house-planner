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

        return rooms.EnumerateArray().Any(room =>
            room.ValueKind == JsonValueKind.Object &&
            room.TryGetProperty("room_type", out var type) && type.ValueKind == JsonValueKind.String &&
            type.GetString() == "bathroom_attached");
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
        return HasRoomType(layout, "balcony");
    }

    public static bool HasVeranda(JsonElement layout)
    {
        return HasRoomType(layout, "veranda");
    }

    public static bool HasOffice(JsonElement layout)
    {
        return HasRoomType(layout, "home_office");
    }

    public static bool HasUtilityRoom(JsonElement layout)
    {
        return HasRoomType(layout, "utility") || HasRoomType(layout, "utility_room");
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
            return siteFeatures.GetArrayLength() > 0;
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
}
