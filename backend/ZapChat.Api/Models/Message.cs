using ZapChat.Api.Enums;

namespace ZapChat.Api.Models;

public class Message
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string? Content { get; set; }
    public MessageType Type { get; set; } = MessageType.Text;
    public int? ReplyToId { get; set; }
    public bool IsEdited { get; set; } = false;
    public bool IsDeleted { get; set; } = false;
    public bool DeletedForAll { get; set; } = false;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? EditedAt { get; set; }

    // Navigation
    public Conversation Conversation { get; set; } = null!;
    public User Sender { get; set; } = null!;
    public Message? ReplyTo { get; set; }
    public ICollection<Message> Replies { get; set; } = [];
    public ICollection<MessageAttachment> Attachments { get; set; } = [];
    public ICollection<MessageReadReceipt> ReadReceipts { get; set; } = [];
    public ICollection<MessageReaction> Reactions { get; set; } = [];
}