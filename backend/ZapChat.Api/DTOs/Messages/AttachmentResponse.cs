namespace ZapChat.Api.DTOs.Messages;
public class AttachmentResponse
{
    public Guid Id { get; set; }
    public string FileUrl { get; set; }  = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public int? Width { get; set; }
    public int? Height { get; set; }
}