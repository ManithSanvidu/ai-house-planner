namespace HousePlanner.API.Services;

internal static class SupabaseSignedUrlNormalizer
{
    public static string Normalize(string supabaseUrl, string signedUrl)
    {
        if (Uri.TryCreate(signedUrl, UriKind.Absolute, out var absolute) &&
            (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
            return absolute.ToString();

        var relativePath = $"/{signedUrl.TrimStart('/')}";
        if (relativePath.StartsWith("/object/sign/", StringComparison.OrdinalIgnoreCase))
            return $"{supabaseUrl.TrimEnd('/')}/storage/v1{relativePath}";

        return $"{supabaseUrl.TrimEnd('/')}{relativePath}";
    }
}
