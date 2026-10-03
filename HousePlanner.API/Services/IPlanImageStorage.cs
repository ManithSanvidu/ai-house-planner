namespace HousePlanner.API.Services;

public interface IPlanImageStorage
{
    Task<string> UploadAsync(
        Guid planId,
        Stream content,
        string extension,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string? reference, CancellationToken cancellationToken = default);

    string? GetPublicUrl(string? reference);

    string? NormalizeReference(string? reference);
}
