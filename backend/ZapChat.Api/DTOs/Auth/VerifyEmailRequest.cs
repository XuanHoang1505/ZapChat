namespace ZapChat.Api.DTOs.Auth;
public class VerifyEmailRequest
{
    public string Email { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
}