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
}
