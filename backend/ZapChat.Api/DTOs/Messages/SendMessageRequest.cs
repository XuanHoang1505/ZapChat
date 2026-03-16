using ZapChat.Api.Enums;

namespace ZapChat.Api.DTOs.Messages;
public class SendMessageRequest
{
    public int ConversationId { get; set; }
    public string? Content { get; set; }
    public MessageType Type { get; set; } = MessageType.Text; 
    public int? ReplyToId { get; set; }
    public List<AttachmentRequest>? Attachments { get; set; }
}