namespace ZapChat.Api.Services.Interfaces;

public interface ICloudinaryService
{
    Task<string> UploadImageAsync(IFormFile file, string folder);
    Task<string> UploadVideoAsync(IFormFile file, string folder);
    Task DeleteImageAsync(string publicId);
    Task DeleteVideoAsync(string publicId);
    static string ExtractPublicId(string imageUrl) => throw new NotImplementedException();
}