using ZapChat.Api.DTOs.Auth;
using ZapChat.Api.Enums;
using ZapChat.Api.Models;
namespace ZapChat.Api.Services.Interfaces;
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task SendVerifyEmailAsync(string email);
    Task ReSendOtpAsync(string email, OtpType type);
    Task VerifyEmailAsync(string email, string otp);
    Task SendResetPasswordEmailAsync(string email);
    Task ResetPasswordAsync(string email, string otp, string newPassword);
    Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    Task RevokeTokenAsync(string refreshToken);
    Task<User?> GetUserByEmailAsync(string email);
}