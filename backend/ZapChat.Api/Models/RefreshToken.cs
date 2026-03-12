namespace ZapChat.Api.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public string? ReplacedByToken { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public User User { get; set; } = null!;
}