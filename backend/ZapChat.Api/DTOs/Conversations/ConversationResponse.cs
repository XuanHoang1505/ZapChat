using ZapChat.Api.Enums;

namespace ZapChat.Api.DTOs.Conversations;

public class ConversationResponse
{
    public int Id { get; set; }
    public ConversationType Type { get; set; } = ConversationType.Private;      // private | group
    public string? GroupName { get; set; }
    public string? GroupAvatarUrl { get; set; }
    public DateTime LastActivityAt { get; set; }
    public LastMessageInfo? LastMessage { get; set; }
    public List<MemberInfo> Members { get; set; } = [];
    public int UnreadCount { get; set; }
}



