namespace ZapChat.Api.Models;

using ZapChat.Api.Enums;

public class ConversationMember
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public Guid UserId { get; set; }
    public ConversationMemberRole Role { get; set; } = ConversationMemberRole.Member;
    public string? NicknameInGroup { get; set; }
    public DateTime? LastReadAt { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.Now;
    public DateTime? LeftAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation
    public Conversation Conversation { get; set; } = null!;
    public User User { get; set; } = null!;
}