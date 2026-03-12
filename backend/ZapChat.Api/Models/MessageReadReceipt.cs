namespace ZapChat.Api.Models;

public class MessageReadReceipt
{
    public int Id { get; set; }
    public int MessageId { get; set; }
    public Guid UserId { get; set; }
    public DateTime ReadAt { get; set; } = DateTime.Now;

    // Navigation
    public Message Message { get; set; } = null!;
    public User User { get; set; } = null!;
}