namespace ZapChat.Api.DTOs.Messages;
public class AttachmentRequest
{
    public string FileUrl { get; set; }  = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public int? Width { get; set; }
    public int? Height { get; set; }
}