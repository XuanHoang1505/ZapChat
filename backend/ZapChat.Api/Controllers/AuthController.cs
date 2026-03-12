using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZapChat.Api.Common.Response;
using ZapChat.Api.DTOs.Auth;
using ZapChat.Api.Services.Interfaces;

namespace ZapChat.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
        => _authService = authService;

    // POST api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        var result = await _authService.RegisterAsync(req);
        return Ok(ApiResponse<AuthResponse>.Ok(result,
            "Đăng ký thành công. Vui lòng kiểm tra email để xác thực tài khoản."));
    }

    // POST api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var result = await _authService.LoginAsync(req);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Đăng nhập thành công."));
    }

    // POST api/auth/refresh
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest req)
    {
        var result = await _authService.RefreshTokenAsync(req);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Làm mới token thành công."));
    }

    // POST api/auth/logout
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequest req)
    {
        await _authService.RevokeTokenAsync(req.RefreshToken);
        return Ok(ApiResponse<object>.Ok(null, "Đăng xuất thành công."));
    }

    // POST api/auth/send-verify-email
    [HttpPost("send-verify-email")]
    public async Task<IActionResult> SendVerifyEmail(ForgotPasswordRequest req)
    {
        await _authService.SendVerifyEmailAsync(req.Email);
        return Ok(ApiResponse<object>.Ok(null,
            "Mã OTP đã được gửi tới email của bạn."));
    }

    // POST api/auth/verify-email
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest req)
    {
        await _authService.VerifyEmailAsync(req.Email, req.Otp);
        return Ok(ApiResponse<object>.Ok(null, "Xác thực email thành công."));
    }

    // POST api/auth/forgot-password
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest req)
    {
        await _authService.SendResetPasswordEmailAsync(req.Email);
        return Ok(ApiResponse<object>.Ok(null,
            "Mã OTP đã được gửi tới email của bạn."));
    }

    // POST api/auth/reset-password
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest req)
    {
        await _authService.ResetPasswordAsync(req.Email, req.Otp, req.NewPassword);
        return Ok(ApiResponse<object>.Ok(null, "Đặt lại mật khẩu thành công."));
    }

    // POST api/auth/change-password
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest req)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await _authService.ChangePasswordAsync(userId, req.OldPassword, req.NewPassword);
        return Ok(ApiResponse<object>.Ok(null, "Đổi mật khẩu thành công."));
    }

    // GET api/auth/me
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;

        var user = _authService.GetUserByEmailAsync(email!).Result;

        return Ok(ApiResponse<object>.Ok(new UserInfo
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role
        }, "Thông tin người dùng."));
    }
}
