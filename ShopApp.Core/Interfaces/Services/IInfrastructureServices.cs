namespace ShopApp.Core.Interfaces.Services;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string fileUrl, CancellationToken ct = default);
}

public interface IDateTimeService
{
    DateTime UtcNow { get; }
}

/// <summary>
/// Fire-and-forget view count tracking.
/// Implementations buffer increments and flush to DB asynchronously.
/// </summary>
public interface IViewCountTracker
{
    void Track(Guid itemId);
}

