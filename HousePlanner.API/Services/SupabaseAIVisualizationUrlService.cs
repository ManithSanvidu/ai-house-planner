using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace HousePlanner.API.Services;

public sealed class SupabaseAIVisualizationUrlService : IAIVisualizationUrlService
{
    internal const int SignedUrlLifetimeSeconds = 1800;

    private readonly HttpClient _client;
    private readonly string _supabaseUrl;
    private readonly string _bucket;
    private readonly string _serviceRoleKey;

    public SupabaseAIVisualizationUrlService(HttpClient client, IConfiguration configuration)
    {
        _client = client;
        _supabaseUrl = Require(configuration, "SUPABASE_URL").TrimEnd('/');
        _serviceRoleKey = Require(configuration, "SUPABASE_SERVICE_ROLE_KEY");
        _bucket = (configuration["SUPABASE_AI_VISUALIZATION_BUCKET"]
            ?? Environment.GetEnvironmentVariable("SUPABASE_AI_VISUALIZATION_BUCKET")
            ?? "ai-visualizations").Trim();
        if (string.IsNullOrWhiteSpace(_bucket) || _bucket.Contains('/'))
            throw new InvalidOperationException("SUPABASE_AI_VISUALIZATION_BUCKET must be a valid bucket name.");
    }

    public async Task<string?> GetReadUrlAsync(
        string? reference,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reference)) return null;
        if (Uri.TryCreate(reference, UriKind.Absolute, out var absolute) &&
            (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
            return reference;
        if (reference.StartsWith('/')) return reference;

        var encodedKey = string.Join('/', reference.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString));
        var url = $"{_supabaseUrl}/storage/v1/object/sign/{Uri.EscapeDataString(_bucket)}/{encodedKey}";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
        request.Headers.TryAddWithoutValidation("apikey", _serviceRoleKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { expiresIn = SignedUrlLifetimeSeconds }),
            Encoding.UTF8,
            "application/json");

        using var response = await _client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Visualization signed URL generation failed with HTTP {(int)response.StatusCode}.");

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;
        var signedPath = root.TryGetProperty("signedURL", out var camel)
            ? camel.GetString()
            : root.TryGetProperty("signedUrl", out var pascal) ? pascal.GetString() : null;
        if (string.IsNullOrWhiteSpace(signedPath))
            throw new InvalidOperationException("Visualization signed URL response was invalid.");

        return SupabaseSignedUrlNormalizer.Normalize(_supabaseUrl, signedPath);
    }

    private static string Require(IConfiguration configuration, string key)
    {
        var value = configuration[key] ?? Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"{key} must be configured for visualization storage.")
            : value;
    }
}
