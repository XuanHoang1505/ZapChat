using ZapChat.Api.Enums;

namespace ZapChat.Api.DTOs.Messages;
public class SendMessageRequest
{
    public Guid ConversationId { get; set; }
    public string? Content { get; set; }
    public MessageType Type { get; set; } = MessageType.Text; 
    public Guid? ReplyToId { get; set; }
    public List<AttachmentRequest>? Attachments { get; set; }
}