namespace ZapChat.Api.Services;

using ZapChat.Api.Common.Exceptions;
using ZapChat.Api.Common.Helpers;
using ZapChat.Api.Data;
using ZapChat.Api.DTOs.Auth;
using ZapChat.Api.Models;
using ZapChat.Api.Services.Interfaces;
using ZapChat.Api.Enums;

public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly JwtHelper _jwtHelper;
    private readonly OtpService _otpService;

    public AuthService(AppDbContext dbContext, JwtHelper jwtHelper, OtpService otpService)
    {
        _dbContext = dbContext;
        _jwtHelper = jwtHelper;
        _otpService = otpService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (_dbContext.Users.Any(u => u.Email == request.Email.Trim().ToLower()))
            throw AppException.Conflict("Email đã tồn tại.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim().ToLower(),
            DisplayName = request.FullName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.User,
            IsVerified = false,
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        await _otpService.SendOtpAsync(user.Email, OtpType.Register);

        return await GenerateAuthResponseAsync(user);

    }
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = _dbContext.Users.FirstOrDefault(u => u.Email == request.Email.Trim().ToLower());
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw AppException.BadRequest("Email hoặc mật khẩu không đúng.");

        if (user.IsDeleted)
            throw AppException.Forbidden("Tài khoản đã bị xóa.");

        if (!user.IsVerified)
            throw AppException.BadRequest("Email chưa được xác thực. Vui lòng kiểm tra hộp thư của bạn.");

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var token = _dbContext.RefreshTokens.FirstOrDefault(rt => rt.Token == request.RefreshToken);
        if (token == null || token.IsRevoked)

            throw AppException.BadRequest("Refresh token không hợp lệ.");
        if (token.ExpiresAt < DateTime.Now)
            throw AppException.BadRequest("Refresh token đã hết hạn.");

        if (token.User.IsDeleted)
            throw AppException.Forbidden("Tài khoản đã bị xóa.");

        token.IsRevoked = true;
        var response = await GenerateAuthResponseAsync(token.User);
        token.ReplacedByToken = response.RefreshToken;
        await _dbContext.SaveChangesAsync();

        return response;
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        var token = _dbContext.RefreshTokens.FirstOrDefault(rt => rt.Token == refreshToken);
        if (token == null || token.IsRevoked)
            throw AppException.BadRequest("Refresh token không hợp lệ.");

        token.IsRevoked = true;
        await _dbContext.SaveChangesAsync();
    }

    public Task SendVerifyEmailAsync(string email)
    {
        var user = _dbContext.Users.FirstOrDefault(u => u.Email == email.Trim().ToLower());

        if (user == null)
            throw AppException.NotFound("Email không tồn tại.");

        if (user.IsVerified)
            throw AppException.BadRequest("Email đã được xác thực.");

        return _otpService.SendOtpAsync(email.ToLower(), OtpType.Register);
    }

    public Task ReSendOtpAsync(string email, OtpType type)
    {
        var user = _dbContext.Users.FirstOrDefault(u => u.Email == email.Trim().ToLower());

        if (user == null)
            throw AppException.NotFound("Email không tồn tại.");

        if (type == OtpType.Register && user.IsVerified)
            throw AppException.BadRequest("Email đã được xác thực.");

        if (type == OtpType.ResetPassword && user.IsDeleted)
            throw AppException.Forbidden("Tài khoản đã bị xóa.");

        return _otpService.SendOtpAsync(email.ToLower(), type);
    }

    public async Task VerifyEmailAsync(string email, string otp)
    {
        var user = _dbContext.Users.FirstOrDefault(u => u.Email == email.Trim().ToLower());

        if (user == null)
            throw AppException.NotFound("Email không tồn tại.");

        if (user.IsVerified)
            throw AppException.BadRequest("Email đã được xác thực.");

        var isValid = await _otpService.VerifyOtpAsync(email.ToLower(), otp, OtpType.Register);
        if (!isValid)
            throw AppException.BadRequest("OTP không hợp lệ hoặc đã hết hạn.");

        user.IsVerified = true;
        user.UpdatedAt = DateTime.Now;
        await _dbContext.SaveChangesAsync();
    }

    public async Task SendResetPasswordEmailAsync(string email)
    {
        var user = _dbContext.Users.FirstOrDefault(u => u.Email == email.Trim().ToLower());

        if (user == null)
            throw AppException.NotFound("Email không tồn tại.");

        if (user.IsDeleted)
            throw AppException.Forbidden("Tài khoản đã bị xóa.");

        await _otpService.SendOtpAsync(email.ToLower(), OtpType.ResetPassword);
    }

    public async Task ResetPasswordAsync(string email, string otp, string newPassword)
    {
        var user = _dbContext.Users.FirstOrDefault(u => u.Email == email.Trim().ToLower());

        if (user == null)
            throw AppException.NotFound("Email không tồn tại.");

        if (user.IsDeleted)
            throw AppException.Forbidden("Tài khoản đã bị xóa.");

        var isValid = await _otpService.VerifyOtpAsync(email.ToLower(), otp, OtpType.ResetPassword);
        if (!isValid)
            throw AppException.BadRequest("OTP không hợp lệ hoặc đã hết hạn.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.Now;
        await _dbContext.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = _dbContext.Users.FirstOrDefault(u => u.Id == userId);

        if (user == null)
            throw AppException.NotFound("Người dùng không tồn tại.");

        if (user.IsDeleted)
            throw AppException.Forbidden("Tài khoản đã bị xóa.");

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            throw AppException.BadRequest("Mật khẩu hiện tại không đúng.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.Now;
        await _dbContext.SaveChangesAsync();
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await Task.FromResult(_dbContext.Users.FirstOrDefault(u => u.Email == email.Trim().ToLower()));
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user)
    {
        var accessToken = _jwtHelper.GenerateAccessToken(user);
        var refreshToken = _jwtHelper.GenerateRefreshToken();
        var expiresAt = _jwtHelper.GetRefreshTokenExpiry();

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = expiresAt
        });
        user.IsOnline = true;
        user.UpdatedAt = DateTime.Now;
        user.LastSeenAt = DateTime.Now;
        await _dbContext.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.DisplayName,
                AvatarUrl = user.AvatarUrl,
                Role = user.Role
            }
        };
    }

}