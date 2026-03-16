using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZapChat.Api.Common.Exceptions;
using ZapChat.Api.Common.Response;
using ZapChat.Api.Services;
using ZapChat.Api.Services.Interfaces;

namespace ZapChat.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly ICloudinaryService _cloudinary;

    public UploadController(ICloudinaryService cloudinary)
        => _cloudinary = cloudinary;

    // POST api/upload/image
    [HttpPost("image")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file is null || file.Length == 0)
            throw AppException.BadRequest("File không hợp lệ.");

        var allowed = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowed.Contains(file.ContentType.ToLower()))
            throw AppException.BadRequest("Chỉ chấp nhận jpg, png, gif, webp.");

        if (file.Length > 10 * 1024 * 1024)
            throw AppException.BadRequest("Ảnh không được vượt quá 10MB.");

        var url      = await _cloudinary.UploadImageAsync(file, "zapchat/messages");
        var publicId = CloudinaryService.ExtractPublicId(url);

        return Ok(ApiResponse<object>.Ok(new {
            FileUrl  = url,
            PublicId = publicId,
            FileName = file.FileName,
            FileSize = file.Length,
            MimeType = file.ContentType,
        }, "Upload ảnh thành công."));
    }

    // POST api/upload/video
    [HttpPost("video")]
    public async Task<IActionResult> UploadVideo(IFormFile file)
    {
        if (file is null || file.Length == 0)
            throw AppException.BadRequest("File không hợp lệ.");

        var allowed = new[] { "video/mp4", "video/webm", "video/quicktime" };
        if (!allowed.Contains(file.ContentType.ToLower()))
            throw AppException.BadRequest("Chỉ chấp nhận mp4, webm, mov.");

        if (file.Length > 100 * 1024 * 1024)
            throw AppException.BadRequest("Video không được vượt quá 100MB.");

        var url      = await _cloudinary.UploadVideoAsync(file, "zapchat/videos");
        var publicId = CloudinaryService.ExtractPublicId(url);

        return Ok(ApiResponse<object>.Ok(new {
            FileUrl  = url,
            PublicId = publicId,
            FileName = file.FileName,
            FileSize = file.Length,
            MimeType = file.ContentType,
        }, "Upload video thành công."));
    }

    // POST api/upload/file
    [HttpPost("file")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file is null || file.Length == 0)
            throw AppException.BadRequest("File không hợp lệ.");

        // Chặn file nguy hiểm
        var blocked = new[] { ".exe", ".bat", ".sh", ".cmd", ".msi" };
        var ext     = Path.GetExtension(file.FileName).ToLower();
        if (blocked.Contains(ext))
            throw AppException.BadRequest("Loại file không được phép.");

        if (file.Length > 25 * 1024 * 1024)
            throw AppException.BadRequest("File không được vượt quá 25MB.");

        var url      = await _cloudinary.UploadFileAsync(file, "zapchat/files");
        var publicId = CloudinaryService.ExtractPublicId(url);

        return Ok(ApiResponse<object>.Ok(new {
            FileUrl  = url,
            PublicId = publicId,
            FileName = file.FileName,
            FileSize = file.Length,
            MimeType = file.ContentType,
        }, "Upload file thành công."));
    }
}