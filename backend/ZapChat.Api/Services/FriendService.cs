using Microsoft.EntityFrameworkCore;
using ZapChat.Api.Common.Exceptions;
using ZapChat.Api.Data;
using ZapChat.Api.DTOs.Friends;
using ZapChat.Api.Enums;
using ZapChat.Api.Models;
using ZapChat.Api.Services.Interfaces;

namespace ZapChat.Api.Services;

public class FriendService : IFriendService
{
    private readonly AppDbContext _db;
    public FriendService(AppDbContext db) => _db = db;
    public async Task<List<FriendResponse>> GetFriendsAsync(Guid userId)
    {
        var friends = await _db.Friends
            .Where(f => f.Status == FriendStatus.Accepted
                    && f.AddresseeId == userId || f.RequesterId == userId)
            .Include(f => f.Addressee)
            .Include(f => f.Requester)
            .ToListAsync();

        return friends.Select(f =>
        {
            var other = f.RequesterId == userId ? f.Addressee : f.Requester;
            return BuildResponse(f, other, userId);
        }).ToList();
    }

    public async Task<List<FriendResponse>> GetPendingRequestsAsync(Guid userId)
    {
        var requests = await _db.Friends
            .Where(f => f.Status == FriendStatus.Pending
                    && f.AddresseeId == userId)
            .Include(f => f.Requester)
            .ToListAsync();

        return requests.Select(r =>
            BuildResponse(r, r.Requester, userId)).ToList();
    }

    public async Task<List<FriendResponse>> GetSentRequestsAsync(Guid userId)
    {
        var requests = await _db.Friends
            .Where(f => f.Status == FriendStatus.Pending
                    && f.RequesterId == userId)
            .Include(f => f.Addressee)
            .ToListAsync();

        return requests.Select(r => 
            BuildResponse(r, r.Addressee, userId)).ToList();    
    }

    public async Task<FriendResponse> SendRequestAsync(Guid requesterId, Guid addresseeId)
    {
        if (requesterId == addresseeId)
            throw AppException.BadRequest("Không thể gửi lời mời với chính mình");

        var addressee = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == addresseeId && !u.IsDeleted);

        if (addressee is null)
            throw AppException.BadRequest("Người dùng này không tồn tại");

        var existing = await GetRelationshipRecordAsync(requesterId, addresseeId);
        if (existing is not null)
        {
            switch (existing.Status)
            {
                case FriendStatus.Accepted :
                    throw AppException.Conflict("Hai người đã là bạn bè");
                case FriendStatus.Pending : 
                    if (existing.AddresseeId == requesterId)
                    {
                        existing.Status = FriendStatus.Accepted;
                        existing.UpdatedAt = DateTime.Now;
                        await _db.SaveChangesAsync();
                        return BuildResponse(existing, addressee, requesterId);
                    }  
                    throw AppException.BadRequest("Bạn đã gửi lời mời rồi."); 
                case FriendStatus.Blocked:
                    throw AppException.Forbidden("Không thể gửi lời mời cho người này.");
            }   
        }
        Friend friend = new Friend
        {
          AddresseeId = addresseeId,
          RequesterId = requesterId,
          Status = FriendStatus.Pending
        };

        _db.Friends.Add(friend);
        await _db.SaveChangesAsync();

        return BuildResponse(friend, addressee, requesterId);                  
    }

    public async Task<FriendResponse> AcceptRequestAsync(Guid userId, int friendId)
    {
        var request = await _db.Friends
            .Include(r => r.Requester)
            .FirstOrDefaultAsync(r => r.Id == friendId);

        if (request is null)
            throw AppException.NotFound("Không tìm thấy lời mời");

        if (request.AddresseeId != userId)
            throw AppException.Forbidden("Bạn không có quyền chấp nhận lời mời này.");   

        if( request.Status != FriendStatus.Pending)
            throw AppException.BadRequest("Lời mời này không còn hiệu lực.");

        request.Status = FriendStatus.Accepted;
        await _db.SaveChangesAsync();

        return BuildResponse(request, request.Requester, userId);        
    }

    public async Task DeclineRequestAsync(Guid userId, int friendId)
    {
        var request = await _db.Friends
            .FirstOrDefaultAsync(r => r.Id == friendId);

         if (request is null)
            throw AppException.NotFound("Không tìm thấy lời mời");

        if (request.AddresseeId != userId)
            throw AppException.Forbidden("Bạn không có quyền chấp nhận lời mời này.");   

        if( request.Status != FriendStatus.Pending)
            throw AppException.BadRequest("Lời mời này không còn hiệu lực.");

        _db.Friends.Remove(request);
        await _db.SaveChangesAsync();         
    }

    public async  Task UnfriendAsync(Guid userId, int friendId)
    {
        var friend = await _db.Friends
            .FirstOrDefaultAsync(f => f.Id == friendId 
                                    && f.AddresseeId == userId || f.RequesterId == userId);

        if (friend is null)
            throw AppException.NotFound("Không tìm thấy quan hệ bạn bè.");

        if (friend.Status != FriendStatus.Accepted)  
            throw AppException.BadRequest("Hai người chưa là bạn bè.");

        _db.Friends.Remove(friend);
        await _db.SaveChangesAsync();                                  
    }

    public async Task BlockAsync(Guid userId, int friendId)
    {
        var friend = await _db.Friends
            .FirstOrDefaultAsync(f => f.Id == friendId &&
                                     (f.RequesterId == userId ||
                                      f.AddresseeId == userId));

        if (friend is null)
            throw AppException.NotFound("Không tìm thấy quan hệ.");

        friend.RequesterId = userId;
        friend.AddresseeId = friend.RequesterId == userId
            ? friend.AddresseeId
            : friend.RequesterId;
        friend.Status = FriendStatus.Blocked;

        await _db.SaveChangesAsync();    
    }

    public async  Task UnblockAsync(Guid userId, int friendId)
    {
        var friend = await _db.Friends
            .FirstOrDefaultAsync(f => f.Id == friendId &&
                                    f.RequesterId == userId 
                                    &&f.Status == FriendStatus.Blocked);

        if (friend is null)
            throw AppException.NotFound("Không tìm thấy người bị chặn.");

        friend.Status = FriendStatus.Accepted;
        await _db.SaveChangesAsync();
    }

    public async Task<FriendStatus?> GetRelationshipAsync(Guid userId, Guid targetUserId)
    {
        var record = await GetRelationshipRecordAsync(userId, targetUserId);
        return record?.Status;
    }

    private async Task<Friend?> GetRelationshipRecordAsync(Guid userA, Guid userB)
        => await _db.Friends
            .FirstOrDefaultAsync(f =>
                (f.RequesterId == userA && f.AddresseeId == userB) ||
                (f.RequesterId == userB && f.AddresseeId == userA));

    private static FriendResponse BuildResponse(
       Friend friend, User other, Guid currentUserId) => new()
       {
           Id = friend.Id,
           UserId = other.Id,
           DisplayName = other.DisplayName,
           AvatarUrl = other.AvatarUrl,
           IsOnline = other.IsOnline,
           LastSeenAt = other.LastSeenAt,
           Status = friend.Status,
           Direction = friend.RequesterId == currentUserId ? "sent" : "received",
       };
}