using ZapChat.Api.Enums;

namespace ZapChat.Api.DTOs.Friends;

public class FriendResponse
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public FriendStatus Status { get; set; } = FriendStatus.Pending ; 
    public string Direction { get; set; } = string.Empty; // sent | received
}