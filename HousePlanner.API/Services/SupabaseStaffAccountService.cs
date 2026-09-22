using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HousePlanner.API.Services;

public sealed record SupabaseStaffIdentity(string Uid, string Email);

public interface ISupabaseStaffAccountService
{
    Task<SupabaseStaffIdentity> CreateAsync(string email, string password, string displayName, CancellationToken cancellationToken = default);
    Task DeleteAsync(string uid, CancellationToken cancellationToken = default);
    Task<bool?> IsDisabledAsync(string uid, CancellationToken cancellationToken = default);
    Task SetDisabledAsync(string uid, bool disabled, CancellationToken cancellationToken = default);
    Task UpdateProfileAsync(string uid, string email, string displayName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Calls the Supabase Admin API using the service role key.
/// Requires SUPABASE_URL and SUPABASE_SERVICE_ROLE_KEY environment variables.
/// </summary>
public sealed class SupabaseStaffAccountService : ISupabaseStaffAccountService
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly ILogger<SupabaseStaffAccountService> _logger;

    public SupabaseStaffAccountService(IHttpClientFactory factory, ILogger<SupabaseStaffAccountService> logger)
    {
        _http = factory.CreateClient("SupabaseAdmin");
        _logger = logger;
    }

    /// <summary>
    /// Creates a Supabase Auth user with the given email/password and marks email as confirmed.
    /// Uses the service role key so no confirmation email is sent and the account is immediately usable.
    /// </summary>
    public async Task<SupabaseStaffIdentity> CreateAsync(
        string email, string password, string displayName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Supabase Admin] Creating staff user. Email: {Email}, passwordPresent: {Present}, passwordLength: {Len}",
            email, !string.IsNullOrEmpty(password), password.Length);

        var body = new
        {
            email,
            password,
            email_confirm = true,
            user_metadata = new { full_name = displayName }
        };

        using var response = await PostAsync("admin/users", body, cancellationToken);
        var doc = await ReadJson(response, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var msg = doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() : response.ReasonPhrase;
            _logger.LogWarning("[Supabase Admin] Create failed: {Status} {Message}", (int)response.StatusCode, msg);

            // Map duplicate email to a recognisable code
            if (response.StatusCode == HttpStatusCode.UnprocessableEntity ||
                (msg?.Contains("already", StringComparison.OrdinalIgnoreCase) == true &&
                 msg.Contains("email", StringComparison.OrdinalIgnoreCase)))
            {
                throw new StaffAccountException("duplicate_email", "An account with this email already exists in Supabase Auth.");
            }

            throw new StaffAccountException("supabase_error", $"Supabase user creation failed: {msg}");
        }

        var uid = doc.RootElement.GetProperty("id").GetString()
            ?? throw new StaffAccountException("supabase_error", "Supabase returned a user without an id.");
        var returnedEmail = doc.RootElement.TryGetProperty("email", out var e) ? e.GetString() ?? email : email;

        _logger.LogInformation("[Supabase Admin] Staff user created. SupabaseUid: {Uid}", uid);
        return new SupabaseStaffIdentity(uid, returnedEmail);
    }

    public async Task DeleteAsync(string uid, CancellationToken cancellationToken = default)
    {
        using var response = await _http.DeleteAsync($"admin/users/{uid}", cancellationToken);
        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
        {
            _logger.LogWarning("[Supabase Admin] Delete failed for uid {Uid}: {Status}", uid, (int)response.StatusCode);
        }
    }

    public async Task<bool?> IsDisabledAsync(string uid, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"admin/users/{uid}", cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        using var doc = await ReadJson(response, cancellationToken);
        return doc.RootElement.TryGetProperty("banned_until", out var b) && b.ValueKind != JsonValueKind.Null;
    }

    public async Task SetDisabledAsync(string uid, bool disabled, CancellationToken cancellationToken = default)
    {
        // Supabase uses ban_duration to disable; "none" to re-enable
        var body = disabled ? new { ban_duration = "876000h" } : (object)new { ban_duration = "none" };
        using var response = await PutAsync($"admin/users/{uid}", body, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var doc = await ReadJson(response, cancellationToken);
            var msg = doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() : response.ReasonPhrase;
            throw new StaffAccountException("status_update_failed", $"Could not update Supabase account status: {msg}");
        }
    }

    public async Task UpdateProfileAsync(string uid, string email, string displayName, CancellationToken cancellationToken = default)
    {
        var body = new { email, user_metadata = new { full_name = displayName } };
        using var response = await PutAsync($"admin/users/{uid}", body, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var doc = await ReadJson(response, cancellationToken);
            var msg = doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() : response.ReasonPhrase;
            if (msg?.Contains("already", StringComparison.OrdinalIgnoreCase) == true &&
                msg.Contains("email", StringComparison.OrdinalIgnoreCase))
            {
                throw new StaffAccountException("duplicate_email", "An account with this email already exists.");
            }
            throw new StaffAccountException("profile_update_failed", $"Could not update Supabase profile: {msg}");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Task<HttpResponseMessage> PostAsync(string path, object body, CancellationToken ct)
        => _http.PostAsync(path, Serialize(body), ct);

    private Task<HttpResponseMessage> PutAsync(string path, object body, CancellationToken ct)
        => _http.PutAsync(path, Serialize(body), ct);

    private static StringContent Serialize(object body)
        => new(JsonSerializer.Serialize(body, _json), Encoding.UTF8, "application/json");

    private static async Task<JsonDocument> ReadJson(HttpResponseMessage response, CancellationToken ct)
    {
        var content = await response.Content.ReadAsStringAsync(ct);
        return string.IsNullOrWhiteSpace(content)
            ? JsonDocument.Parse("{}")
            : JsonDocument.Parse(content);
    }
}
