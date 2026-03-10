namespace ZapChat.Api.Models;

public class MessageReaction
{
    public int Id { get; set; }
    public int MessageId { get; set; }
    public int UserId { get; set; }
    public string Emoji { get; set; } = string.Empty; 
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Message Message { get; set; } = null!;
    public User User { get; set; } = null!;
}