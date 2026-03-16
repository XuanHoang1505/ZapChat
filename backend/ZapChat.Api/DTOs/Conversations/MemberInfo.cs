using ZapChat.Api.Enums;

namespace ZapChat.Api.DTOs.Conversations;
public class MemberInfo
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public ConversationMemberRole Role { get; set; } = ConversationMemberRole.Member;      // admin | member
    public bool IsOnline { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public string? NicknameInGroup { get; set; }
}