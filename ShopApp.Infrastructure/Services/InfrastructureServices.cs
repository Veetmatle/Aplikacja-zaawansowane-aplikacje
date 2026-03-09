using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ShopApp.Core.Interfaces.Services;

namespace ShopApp.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var id = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User?.FindFirstValue("sub");
            return Guid.TryParse(id, out var guid) ? guid : null;
        }
    }

    public string? UserName => User?.FindFirstValue(ClaimTypes.Email);
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;
}

public class DateTimeService : IDateTimeService
{
    public DateTime UtcNow => DateTime.UtcNow;
}

/// <summary>
/// Local filesystem storage - stores files in wwwroot/uploads.
/// TODO: Replace with Azure Blob Storage / S3 in production.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _uploadPath;
    private readonly string _baseUrl;

    public LocalFileStorageService(IHttpContextAccessor httpContextAccessor)
    {
        _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(_uploadPath);
        var request = httpContextAccessor.HttpContext?.Request;
        _baseUrl = request is not null ? $"{request.Scheme}://{request.Host}/uploads" : "/uploads";
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        var uniqueFileName = $"{Guid.NewGuid():N}_{fileName}";
        var filePath = Path.Combine(_uploadPath, uniqueFileName);

        await using var fs = new FileStream(filePath, FileMode.Create);
        await fileStream.CopyToAsync(fs, ct);

        return $"{_baseUrl}/{uniqueFileName}";
    }

    public Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        var fileName = Path.GetFileName(new Uri(fileUrl).LocalPath);
        var filePath = Path.Combine(_uploadPath, fileName);
        if (File.Exists(filePath)) File.Delete(filePath);
        return Task.CompletedTask;
    }
}
