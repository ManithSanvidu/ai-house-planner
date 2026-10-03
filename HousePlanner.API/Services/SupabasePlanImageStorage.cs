using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace HousePlanner.API.Services;

public sealed class SupabasePlanImageStorage : IPlanImageStorage
{
    private readonly HttpClient _client;
    private readonly string _supabaseUrl;
    private readonly string _bucket;
    private readonly string _serviceRoleKey;

    public SupabasePlanImageStorage(HttpClient client, IConfiguration configuration)
    {
        _client = client;
        _supabaseUrl = Require(configuration, "SUPABASE_URL").TrimEnd('/');
        _serviceRoleKey = Require(configuration, "SUPABASE_SERVICE_ROLE_KEY");
        _bucket = (configuration["SUPABASE_PLAN_IMAGE_BUCKET"]
            ?? Environment.GetEnvironmentVariable("SUPABASE_PLAN_IMAGE_BUCKET")
            ?? "plan-library").Trim();
        if (string.IsNullOrWhiteSpace(_bucket) || _bucket.Contains('/'))
            throw new InvalidOperationException("SUPABASE_PLAN_IMAGE_BUCKET must be a valid bucket name.");
    }

    public async Task<string> UploadAsync(
        Guid planId,
        Stream content,
        string extension,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var normalizedExtension = extension.Trim().TrimStart('.').ToLowerInvariant();
        var objectKey = $"{planId:D}/{Guid.NewGuid():D}.{normalizedExtension}";
        using var request = CreateRequest(HttpMethod.Post, ObjectEndpoint(objectKey));
        request.Headers.TryAddWithoutValidation("x-upsert", "false");
        request.Content = new StreamContent(content);
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

        using var response = await _client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Plan image upload failed with HTTP {(int)response.StatusCode}.");

        return objectKey;
    }

    public async Task DeleteAsync(string? reference, CancellationToken cancellationToken = default)
    {
        var objectKey = NormalizeReference(reference);
        if (string.IsNullOrWhiteSpace(objectKey) || objectKey.StartsWith('/')) return;

        using var request = CreateRequest(
            HttpMethod.Delete,
            $"{_supabaseUrl}/storage/v1/object/{Uri.EscapeDataString(_bucket)}");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { prefixes = new[] { objectKey } }),
            Encoding.UTF8,
            "application/json");

        using var response = await _client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Plan image deletion failed with HTTP {(int)response.StatusCode}.");
    }

    public string? GetPublicUrl(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference)) return null;
        if (Uri.TryCreate(reference, UriKind.Absolute, out var absolute) &&
            (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
            return reference;
        if (reference.StartsWith('/')) return reference;

        return $"{PublicPrefix()}/{EncodeObjectKey(reference)}";
    }

    public string? NormalizeReference(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference)) return null;
        if (reference.StartsWith('/')) return reference;

        var publicPrefix = PublicPrefix() + "/";
        if (reference.StartsWith(publicPrefix, StringComparison.OrdinalIgnoreCase))
            return Uri.UnescapeDataString(reference[publicPrefix.Length..]);

        return reference;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
        request.Headers.TryAddWithoutValidation("apikey", _serviceRoleKey);
        return request;
    }

    private string ObjectEndpoint(string objectKey) =>
        $"{_supabaseUrl}/storage/v1/object/{Uri.EscapeDataString(_bucket)}/{EncodeObjectKey(objectKey)}";

    private string PublicPrefix() =>
        $"{_supabaseUrl}/storage/v1/object/public/{Uri.EscapeDataString(_bucket)}";

    private static string EncodeObjectKey(string objectKey) =>
        string.Join('/', objectKey.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString));

    private static string Require(IConfiguration configuration, string key)
    {
        var value = configuration[key] ?? Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"{key} must be configured for plan image storage.")
            : value;
    }
}
