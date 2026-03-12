using Microsoft.EntityFrameworkCore;
using ZapChat.Api.Data;
using ZapChat.Api.Enums;
using ZapChat.Api.Models;
using ZapChat.Api.Services.Interfaces;

namespace ZapChat.Api.Services;

public class OtpService
{
    private const int OtpExpiryMinutes = 5;

    private readonly AppDbContext _db;
    private readonly ISendMailService _email;

    public OtpService(AppDbContext db, ISendMailService email)
    {
        _db = db;
        _email = email;
    }

    public async Task SendOtpAsync(string toEmail, OtpType type)
    {
        var oldOtps = await _db.OtpCodes
            .Where(o => o.Email == toEmail && o.Type == type && !o.IsUsed)
            .ToListAsync();
        _db.OtpCodes.RemoveRange(oldOtps);

        var code = GenerateOtp();

        _db.OtpCodes.Add(new OtpCode
        {
            Email = toEmail,
            Code = code,
            Type = type,
            ExpiresAt = DateTime.Now.AddMinutes(OtpExpiryMinutes),
        });

        await _db.SaveChangesAsync();
        await SendOtpEmailAsync(toEmail, code, type);
    }

    // ── Xác thực OTP ─────────────────────────────────
    public async Task<bool> VerifyOtpAsync(string email, string code, OtpType type)
    {
        var otp = await _db.OtpCodes
            .Where(o => o.Email == email
                     && o.Code == code
                     && o.Type == type
                     && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (otp is null) return false;
        if (otp.ExpiresAt < DateTime.Now) return false;

        // Đánh dấu đã dùng — không dùng lại được
        otp.IsUsed = true;
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Private helpers ───────────────────────────────
    private static string GenerateOtp()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    private async Task SendOtpEmailAsync(string to, string code, OtpType type)
    {
        var subject = type == OtpType.Register
            ? "ZapChat — Xác thực email của bạn"
            : "ZapChat — Mã đặt lại mật khẩu";

        var message = type == OtpType.Register
            ? "Bạn đã đăng ký tài khoản ZapChat. Dùng mã bên dưới để xác thực email."
            : "Bạn đã yêu cầu đặt lại mật khẩu. Dùng mã bên dưới để tiếp tục.";

        var body = $$"""
            <html>
            <head>
            <style>
                body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
                .container { max-width: 500px; margin: 0 auto; padding: 32px;
                            border: 1px solid #ddd; border-radius: 12px; }
                .otp-box { font-size: 36px; font-weight: bold; color: #1565C0;
                        letter-spacing: 8px; padding: 20px;
                        background: #E3F2FD; border-radius: 8px;
                        text-align: center; margin: 24px 0; }
                .footer { margin-top: 24px; font-size: 12px;
                        color: #aaa; text-align: center; }
            </style>
            </head>
            <body>
                <div class='container'>
                    <h2 style='color:#1565C0;'>⚡ ZapChat</h2>
                    <h3>{{subject}}</h3>
                    <p>{{message}}</p>
                    <div class='otp-box'>{{code}}</div>
                    <p style='color:#888;font-size:13px;'>
                        Mã có hiệu lực trong <strong>{{OtpExpiryMinutes}} phút</strong>.
                        Không chia sẻ mã này với bất kỳ ai.
                    </p>
                    <div class='footer'>
                        Email tự động từ ZapChat. Vui lòng không trả lời email này.
                    </div>
                </div>
            </body>
            </html>
            """;

        await _email.SendEmailAsync(to, subject, body);
    }
}
