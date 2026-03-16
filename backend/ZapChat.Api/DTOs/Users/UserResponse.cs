namespace ZapChat.Api.DTOs.Users;

public class UserResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; }       = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime CreatedAt { get; set; }
}