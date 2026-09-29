using System;
using System.Text.Json;

public sealed class StartDesignRequest
{
    public string LandSizeCategory { get; init; } = string.Empty;
    public int LandSizePerches { get; init; }
    public int Bedrooms { get; init; }
    public int Bathrooms { get; init; }
    public string HouseType { get; init; } = string.Empty;
}

var json = "{\"landSizeCategory\":\"medium\",\"landSizePerches\":15,\"bedrooms\":2,\"bathrooms\":1,\"houseType\":\"modern\"}";
var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var req = JsonSerializer.Deserialize<StartDesignRequest>(json, options);

Console.WriteLine($"Bedrooms: {req.Bedrooms}");
Console.WriteLine($"Bathrooms: {req.Bathrooms}");
