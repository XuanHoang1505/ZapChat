using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ZapChat.Api.Services.Interfaces;

namespace ZapChat.Api.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;

    public CloudinaryService(Cloudinary cloudinary, ILogger<CloudinaryService> logger)
    {
        _cloudinary = cloudinary;
        _logger     = logger;
    }

    // ── Upload ảnh ────────────────────────────────────
    public async Task<string> UploadImageAsync(IFormFile file, string folder)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File không hợp lệ!");

        var publicId = $"{folder}/{Guid.NewGuid()}";

        using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams {
            File           = new FileDescription(file.FileName, stream),
            PublicId       = publicId,
            UseFilename    = true,
            UniqueFilename = true
        };

        var result = await _cloudinary.UploadAsync(uploadParams);
        _logger.LogInformation("Upload ảnh thành công: {Url}", result.SecureUrl);

        return result.SecureUrl.ToString();
    }

    // ── Upload video ──────────────────────────────────
    public async Task<string> UploadVideoAsync(IFormFile file, string folder)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File không hợp lệ!");

        var publicId = $"{folder}/{Guid.NewGuid()}";

        using var stream = file.OpenReadStream();
        var uploadParams = new VideoUploadParams {
            File           = new FileDescription(file.FileName, stream),
            PublicId       = publicId,
            UseFilename    = true,
            UniqueFilename = true
        };

        var result = await _cloudinary.UploadAsync(uploadParams);
        _logger.LogInformation("Upload video thành công: {Url}", result.SecureUrl);

        return result.SecureUrl.ToString();
    }

    // ── Xoá ảnh ──────────────────────────────────────
    public async Task DeleteImageAsync(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            _logger.LogWarning("Bỏ qua xoá ảnh — publicId rỗng.");
            return;
        }

        var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId));
        _logger.LogInformation("Xoá ảnh - publicId: {PublicId}, kết quả: {Result}",
            publicId, result.Result);
    }

    // ── Xoá video ────────────────────────────────────
    public async Task DeleteVideoAsync(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            _logger.LogWarning("Bỏ qua xoá video — publicId rỗng.");
            return;
        }

        var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId) {
            ResourceType = ResourceType.Video
        });
        _logger.LogInformation("Xoá video - publicId: {PublicId}, kết quả: {Result}",
            publicId, result.Result);
    }

    public async Task<string> UploadFileAsync(IFormFile file, string folder)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File không hợp lệ!");

        var publicId = $"{folder}/{Guid.NewGuid()}";

        using var stream = file.OpenReadStream();
        var uploadParams = new RawUploadParams {
            File           = new FileDescription(file.FileName, stream),
            PublicId       = publicId,
            UseFilename    = true,
            UniqueFilename = true
        };

        var result = await _cloudinary.UploadAsync(uploadParams);
        _logger.LogInformation("Upload file thành công: {Url}", result.SecureUrl);

        return result.SecureUrl.ToString();
    }

    // ── Extract PublicId từ URL Cloudinary ────────────
    public static string? ExtractPublicId(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) return null;

        var startIndex = imageUrl.IndexOf("/upload/") + 8;
        if (startIndex == 7) return null;

        var filePath = imageUrl[startIndex..];
        var parts    = filePath.Split('/');

        // Bỏ phần version (v1701234567) nếu có
        if (parts.Length > 1 && parts[0].StartsWith('v'))
            return string.Join("/", parts[1..]).Split('.')[0];

        return filePath.Split('.')[0];
    }
}