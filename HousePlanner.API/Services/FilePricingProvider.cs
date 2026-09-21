using System.Text.Json;
using HousePlanner.API.Models;
using HousePlanner.API.Options;
using Microsoft.Extensions.Options;

namespace HousePlanner.API.Services;

public sealed class FilePricingProvider : IExternalPricingProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ExternalPricingOptions _options;
    private readonly IHostEnvironment _environment;

    public FilePricingProvider(IOptions<ExternalPricingOptions> options, IHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public string Name => _options.ProviderName;

    public async Task<IReadOnlyList<ExternalPriceRecord>> GetPricesAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.FilePath))
        {
            throw new ExternalPricingProviderException("ExternalPricing:FilePath is required for the file provider.");
        }

        var path = Path.IsPathRooted(_options.FilePath)
            ? Path.GetFullPath(_options.FilePath)
            : Path.GetFullPath(Path.Combine(_environment.ContentRootPath, _options.FilePath));

        if (!File.Exists(path))
        {
            throw new ExternalPricingProviderException("The configured approved pricing feed was not found.");
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var feed = await JsonSerializer.DeserializeAsync<ExternalPricingFeed>(stream, JsonOptions, cancellationToken);
            return feed?.Records ?? [];
        }
        catch (JsonException ex)
        {
            throw new ExternalPricingProviderException("The approved pricing feed contains invalid JSON.", ex);
        }
        catch (IOException ex)
        {
            throw new ExternalPricingProviderException("The approved pricing feed could not be read.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new ExternalPricingProviderException("The approved pricing feed could not be read.", ex);
        }
    }
}
