namespace HousePlanner.API.Services;

public interface IAIVisualizationUrlService
{
    Task<string?> GetReadUrlAsync(
        string? reference,
        CancellationToken cancellationToken = default);
}
