namespace ZapChat.Api.Models;

using ZapChat.Api.Enums;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsVerified { get; set; } = false;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public bool IsOnline { get; set; } = false;
    public DateTime? LastSeenAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public bool IsDeleted { get; set; } = false;
    public UserRole Role { get; set; } = UserRole.User;

    // Navigation
    public ICollection<ConversationMember> ConversationMembers { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
    public ICollection<MessageReaction> Reactions { get; set; } = [];
    public ICollection<Friend> SentFriendRequests { get; set; } = [];
    public ICollection<Friend> ReceivedFriendRequests { get; set; } = [];
}