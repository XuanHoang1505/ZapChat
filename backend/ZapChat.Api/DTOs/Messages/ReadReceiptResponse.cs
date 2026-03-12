namespace ZapChat.Api.DTOs.Messages;

public class ReadReceiptResponse
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime ReadAt { get; set; }
}