using ZapChat.Api.DTOs.Friends;
using ZapChat.Api.Enums;

namespace ZapChat.Api.Services.Interfaces;

public interface IFriendService
{
    Task<List<FriendResponse>> GetFriendsAsync(Guid userId);
    Task<List<FriendResponse>> GetPendingRequestsAsync(Guid userId);
    Task<List<FriendResponse>> GetSentRequestsAsync(Guid userId);

    // Hành động
    Task<FriendResponse> SendRequestAsync(Guid requesterId, Guid addresseeId);
    Task<FriendResponse> AcceptRequestAsync(Guid userId, int friendId);
    Task DeclineRequestAsync(Guid userId, int friendId);
    Task UnfriendAsync(Guid userId, int friendId);
    Task BlockAsync(Guid userId, int friendId);
    Task UnblockAsync(Guid userId, int friendId);

    // Kiểm tra
    Task<FriendStatus?> GetRelationshipAsync(Guid userId, Guid targetUserId);
}