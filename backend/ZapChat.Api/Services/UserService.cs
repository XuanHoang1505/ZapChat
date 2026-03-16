using Microsoft.EntityFrameworkCore;
using ZapChat.Api.Common.Exceptions;
using ZapChat.Api.Data;
using ZapChat.Api.DTOs.Users;
using ZapChat.Api.Models;
using ZapChat.Api.Services.Interfaces;

namespace ZapChat.Api.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly ICloudinaryService _cloudinary;

    public UserService(AppDbContext db, ICloudinaryService cloudinary)
    {
        _db = db;
        _cloudinary = cloudinary;
    }

    public async Task<UserResponse> GetProfileAsync(Guid userId)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user is null)
            throw AppException.NotFound("Người dùng không tồn tại");

        return BuildResponse(user);
    }

    public async Task<UserResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest req)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user is null)
            throw AppException.NotFound("Người dùng không tồn tại");

        if (!string.IsNullOrWhiteSpace(req.DisplayName))
            user.DisplayName = req.DisplayName.Trim();

        if (req.Bio is not null)
            user.Bio = req.Bio.Trim();
        
        if (req.Avatar is not null && req.Avatar.Length > 0)
        {
            if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
            {
                var oldPublicId = CloudinaryService.ExtractPublicId(user.AvatarUrl);
                if (oldPublicId is not null)
                    await _cloudinary.DeleteImageAsync(oldPublicId);
            }

            user.AvatarUrl = await _cloudinary.UploadImageAsync(req.Avatar, "zapchat/avatars");
        }

        user.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();

        return BuildResponse(user);
    }

    public async Task<List<UserResponse>> SearchUsersAsync(string keyword, Guid currentUserId)
    {
        if (string.IsNullOrWhiteSpace(keyword) || keyword.Trim().Length < 2)
            throw AppException.BadRequest("Từ khoá tìm kiếm phải có ít nhất 2 ký tự.");

        keyword = keyword.Trim().ToLower();

        var users = await _db.Users
            .Where(u => !u.IsDeleted
                     && u.Id != currentUserId
                     && (u.DisplayName.ToLower().Contains(keyword)
                      || u.Email.ToLower().Contains(keyword)))
            .OrderBy(u => u.DisplayName)
            .Take(20)
            .ToListAsync();

        return users.Select(BuildResponse).ToList();    
    }

    private static UserResponse BuildResponse(User user)
        => new()
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            IsOnline = user.IsOnline,
            LastSeenAt = user.LastSeenAt,
            CreatedAt = user.CreatedAt,
        };
}