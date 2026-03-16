using ZapChat.Api.Enums;

namespace ZapChat.Api.DTOs.Conversations;
public class LastMessageInfo
{
    public int Id { get; set; }
    public string? Content { get; set; }
    public MessageType Type { get; set; } = MessageType.Text;
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}