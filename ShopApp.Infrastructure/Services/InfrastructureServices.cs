using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
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
/// Local filesystem storage — stores files in a configurable upload directory.
/// Includes path traversal protection via filename sanitization.
/// Configure via FileStorage:BasePath in appsettings.json.
/// TODO: Replace with Azure Blob Storage / S3 in production.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _uploadPath;
    private readonly string _baseUrl;

    public LocalFileStorageService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        var configuredPath = configuration["FileStorage:BasePath"];
        _uploadPath = !string.IsNullOrWhiteSpace(configuredPath)
            ? configuredPath
            : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

        Directory.CreateDirectory(_uploadPath);

        var request = httpContextAccessor.HttpContext?.Request;
        _baseUrl = request is not null ? $"{request.Scheme}://{request.Host}/uploads" : "/uploads";
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        // Sanitize filename — prevent path traversal
        var sanitizedName = SanitizeFileName(fileName);
        var uniqueFileName = $"{Guid.NewGuid():N}_{sanitizedName}";
        var filePath = Path.Combine(_uploadPath, uniqueFileName);

        // Verify resolved path is still inside upload directory
        var fullPath = Path.GetFullPath(filePath);
        var fullUploadPath = Path.GetFullPath(_uploadPath);
        if (!fullPath.StartsWith(fullUploadPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid file path detected (path traversal attempt).");

        await using var fs = new FileStream(filePath, FileMode.Create);
        await fileStream.CopyToAsync(fs, ct);

        return $"{_baseUrl}/{uniqueFileName}";
    }

    public Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        var fileName = SanitizeFileName(Path.GetFileName(new Uri(fileUrl).LocalPath));
        var filePath = Path.Combine(_uploadPath, fileName);

        // Verify resolved path is still inside upload directory
        var fullPath = Path.GetFullPath(filePath);
        var fullUploadPath = Path.GetFullPath(_uploadPath);
        if (!fullPath.StartsWith(fullUploadPath, StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask; // silently reject traversal attempts

        if (File.Exists(filePath)) File.Delete(filePath);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Strip directory separators and relative path components from filename.
    /// Only keeps the actual filename part.
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        // Get only the filename part (removes any directory components)
        var sanitized = Path.GetFileName(fileName);

        // Remove any remaining invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        sanitized = string.Concat(sanitized.Where(c => !invalidChars.Contains(c)));

        // Ensure it's not empty after sanitization
        if (string.IsNullOrWhiteSpace(sanitized))
            sanitized = "unnamed";

        return sanitized;
    }
}
