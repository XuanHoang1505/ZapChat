namespace ZapChat.Api.Models;
public class MessageAttachment
{
    public int Id { get; set; }
    public int MessageId { get; set; }
    public string FileUrl { get; set; } = string.Empty;     // Cloudinary URL
    public string PublicId { get; set; } = string.Empty;    // Cloudinary PublicId (để xoá)
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }                      // bytes
    public string MimeType { get; set; } = string.Empty;    // "image/jpeg", "application/pdf"
    public int? Width { get; set; }                         // chỉ dùng cho ảnh/video
    public int? Height { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Message Message { get; set; } = null!;
}