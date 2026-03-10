namespace ZapChat.Api.Models;
using ZapChat.Api.Enums;

public class Friend
{
    public int Id { get; set; }
    public int RequesterId { get; set; }    // người gửi lời mời
    public int AddresseeId { get; set; }    // người nhận lời mời
    public FriendStatus Status { get; set; } = FriendStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User Requester { get; set; } = null!;
    public User Addressee { get; set; } = null!;
}