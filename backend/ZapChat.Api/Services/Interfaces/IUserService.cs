using ZapChat.Api.DTOs.Users;

namespace ZapChat.Api.Services.Interfaces;

public interface IUserService
{
    Task<UserResponse> GetProfileAsync(Guid userId);
    Task<UserResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest req);
    Task<List<UserResponse>> SearchUsersAsync(string keyword, Guid currentUserId);
}