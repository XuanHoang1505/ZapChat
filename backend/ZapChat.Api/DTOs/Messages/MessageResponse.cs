using ZapChat.Api.Enums;

namespace ZapChat.Api.DTOs.Messages;
public class MessageResponse
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; }   = string.Empty;
    public string? SenderAvatar { get; set; }
    public string? Content { get; set; }
    public MessageType Type { get; set; } = MessageType.Text;
    public Guid? ReplyToId { get; set; }
    public bool IsEdited { get; set; }
    public DateTime SentAt { get; set; }
    public List<AttachmentResponse> Attachments { get; set; } = [];
    public List<ReactionResponse> Reactions { get; set; }     = [];
    public List<ReadReceiptResponse> ReadBy { get; set; }     = [];
}