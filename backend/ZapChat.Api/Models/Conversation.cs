namespace ZapChat.Api.Models;

using ZapChat.Api.Enums;
public class Conversation
{
    public int Id { get; set; }
    public ConversationType Type { get; set; } = ConversationType.Private;
    public string? GroupName { get; set; }
    public string? GroupAvatarUrl { get; set; }
    public Guid CreatedByUserId { get; set; }
    public int? LastMessageId { get; set; }
    public DateTime LastActivityAt { get; set; } = DateTime.Now;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsDeleted { get; set; } = false;

    // Navigation
    public User CreatedBy { get; set; } = null!;
    public Message? LastMessage { get; set; }
    public ICollection<ConversationMember> Members { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
}