namespace ZapChat.Api.Services;

using CloudinaryDotNet;
using Microsoft.EntityFrameworkCore;
using ZapChat.Api.Common.Exceptions;
using ZapChat.Api.Data;
using ZapChat.Api.DTOs.Conversations;
using ZapChat.Api.DTOs.Messages;
using ZapChat.Api.Enums;
using ZapChat.Api.Models;
using ZapChat.Api.Services.Interfaces;

public class ConversationService : IConversationService
{
    private readonly AppDbContext _db;
    private readonly ICloudinaryService _cloudinary;
    public ConversationService(AppDbContext db, ICloudinaryService cloudinary)
    {
        _db = db;
        _cloudinary = cloudinary;
    }
    public async Task<ConversationResponse> CreatePrivateAsync(Guid userId, CreatePrivateConversationRequest req)
    {
        if (userId == req.TargetUserId)
            throw AppException.BadRequest("Không thể tạo conversation với chính mình.");

        var targetUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == req.TargetUserId && !u.IsDeleted);
        if (targetUser is null)
            throw AppException.NotFound("Người dùng không tồn tại");

        var existing = await _db.Conversations
                            .Where(c => c.Type == ConversationType.Private && !c.IsDeleted)
                            .Where(c => c.Members.Any(m => m.UserId == userId && m.LeftAt == null)
                                    && c.Members.Any(m => m.UserId == req.TargetUserId && m.LeftAt == null))
                            .FirstOrDefaultAsync();

        if (existing is not null)
            return await BuildResponseAsync(existing, userId);

        var conversation = new Conversation
        {
            Type = ConversationType.Private,
            CreatedByUserId = userId,
            LastActivityAt = DateTime.Now
        };
        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync();

        _db.ConversationMembers.AddRange([
            new ConversationMember{
                ConversationId = conversation.Id,
                UserId = userId,
                Role = ConversationMemberRole.Member
            },
            new ConversationMember{
                ConversationId = conversation.Id,
                UserId = req.TargetUserId,
                Role = ConversationMemberRole.Member
            },
        ]);
        await _db.SaveChangesAsync();
        return await BuildResponseAsync(conversation, userId);
    }

    public async Task<ConversationResponse> CreateGroupAsync(
        Guid userId, CreateGroupConversationRequest req)
    {
        var memberIds = req.MemberIds
                        .Distinct()
                        .Where(id => id != userId)
                        .ToList();
        if (memberIds.Count < 2)
            throw AppException.BadRequest("Nhóm cần ít nhất 2 thành viên");

        var validUsers = await _db.Users
                            .Where(u => memberIds.Contains(u.Id) && !u.IsDeleted)
                            .Select(u => u.Id)
                            .ToListAsync();

        var invalidUsers = memberIds.Except(validUsers).ToList();
        if (invalidUsers.Count > 0)
            throw AppException.BadRequest("Một số người dùng không tồn tại.");

        var conversation = new Conversation
        {
            Type = ConversationType.Group,
            GroupName = req.GroupName,
            CreatedByUserId = userId,
            LastActivityAt = DateTime.Now
        };

        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync();

        var members = new List<ConversationMember>
        {
            new()
            {
                Role = ConversationMemberRole.Admin,
                UserId = userId,
                ConversationId = conversation.Id
            }
        };

        members.AddRange(memberIds.Select(id => new ConversationMember
        {
            UserId = id,
            Role = ConversationMemberRole.Member,
            ConversationId = conversation.Id
        }));

        _db.ConversationMembers.AddRange(members);
        await _db.SaveChangesAsync();

        return await BuildResponseAsync(conversation, userId);

    }

    public async Task<List<ConversationResponse>> GetMyConversationsAsync(Guid userId)
    {
        var conversations = await _db.Conversations
                            .Where(c => !c.IsDeleted
                                && c.Members.Any(m => m.UserId == userId && m.LeftAt == null))
                            .OrderByDescending(c => c.LastActivityAt)    
                            .ToListAsync();

        var result = new List<ConversationResponse>();
        foreach(var conv in conversations)
            result.Add(await BuildResponseAsync(conv, userId));      

        return result;            
    }

    public async Task<ConversationResponse> GetByIdAsync(int conversationId, Guid userId)
    {
        var conversation = await _db.Conversations
            .FirstOrDefaultAsync(c => !c.IsDeleted && c.Id == conversationId);

        if (conversation is null)
            throw AppException.NotFound("Không tìm thấy cuộc trò chuyện");

        var isMember = await _db.ConversationMembers
            .AnyAsync(cm => cm.ConversationId == conversationId 
                && cm.UserId == userId 
                && cm.LeftAt == null);
        if (!isMember)
            throw AppException.Forbidden("Bạn không có quyền xem cuộc trò chuyện này");

        return await BuildResponseAsync(conversation, userId);
    }

    public async Task<ConversationResponse> UpdateGroupAsync(int conversationId, Guid userId, UpdateGroupRequest req)
    {
        var conversation = await GetGroupOrThrowAsync(conversationId);
        await RequireAdminAsync(conversationId, userId);

        if (!string.IsNullOrWhiteSpace(req.GroupName))
            conversation.GroupName = req.GroupName.Trim();
        
        if (req.GroupAvatar is not null && req.GroupAvatar.Length > 0)
        {
            if (!string.IsNullOrWhiteSpace(conversation.GroupAvatarUrl))
            {
                var oldPublicId = CloudinaryService.ExtractPublicId(conversation.GroupAvatarUrl);
                if (!string.IsNullOrWhiteSpace(oldPublicId))
                    await _cloudinary.DeleteImageAsync(oldPublicId);
            }

            conversation.GroupAvatarUrl = await _cloudinary.UploadImageAsync(req.GroupAvatar, "zapchat/groups");
        }

        await _db.SaveChangesAsync();   
        return await BuildResponseAsync(conversation, userId);
    }

    public async Task AddMembersAsync(int conversationId, Guid userId, AddMembersRequest req)
    {
        var conversation = await GetGroupOrThrowAsync(conversationId);
        foreach(var targetId in req.UserIds)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Id == targetId && !u.IsDeleted);
            if (user is null)
                continue;
            
            var existingMember = await _db.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId
                                        && cm.UserId == targetId);
            if (existingMember is not null)
            {
                if (existingMember.LeftAt.HasValue)
                {
                    existingMember.LeftAt = null;
                    existingMember.JoinedAt = DateTime.Now;
                    existingMember.Role = ConversationMemberRole.Member;
                }
                continue;
            }

            _db.ConversationMembers
                .Add(new ConversationMember
                {
                   JoinedAt = DateTime.Now,
                   Role = ConversationMemberRole.Member,
                   ConversationId = conversationId,
                   UserId = targetId 
                });
            await _db.SaveChangesAsync();
        }
    }

    public async Task RemoveMemberAsync(int conversationId, Guid adminId, Guid targetUserId)
    {
        await GetGroupOrThrowAsync(conversationId);
        await RequireAdminAsync(conversationId, adminId);

        if (adminId == targetUserId)
            throw AppException.BadRequest("Không thể xóa chính mình ra khỏi nhóm");

        var member = await _db.ConversationMembers
            .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId
                                    && cm.UserId == targetUserId 
                                    && cm.LeftAt == null);    

        if (member is null)
            throw AppException.NotFound("Thành viên không tồn tại trong nhóm");

        if (member.Role == ConversationMemberRole.Admin)
            throw AppException.Forbidden("Không thể xóa Admin khác");

        member.LeftAt = DateTime.Now;
        await _db.SaveChangesAsync();    

    }

    public async Task LeaveGroupAsync(int conversationId, Guid userId)
    {
        var conversation = await GetGroupOrThrowAsync(conversationId);
        var member = await _db.ConversationMembers
            .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId
                                    && cm.UserId == userId 
                                    && cm.LeftAt == null);    
        if (member is null)
            throw AppException.NotFound("Bạn không phải là thành viên nhóm");

        var activeMembers = await _db.ConversationMembers
            .CountAsync(cm => cm.ConversationId == conversationId && cm.LeftAt == null);
        if (activeMembers == 2)
        {
            conversation.IsDeleted = true;
        }

        if (member.Role == ConversationMemberRole.Admin)
        {
            var otherAdmin = await _db.ConversationMembers
                .AnyAsync(cm => cm.ConversationId == conversationId
                                && cm.Role == ConversationMemberRole.Admin
                                && cm.UserId != userId
                                && cm.LeftAt == null);
            if (!otherAdmin)
            {
                 var otherMembers = await _db.ConversationMembers
                    .AnyAsync(m => m.ConversationId == conversationId
                                && m.UserId != userId
                                && m.LeftAt == null);

                if (otherMembers)
                    throw AppException.BadRequest(
                        "Bạn là admin duy nhất. Hãy chuyển quyền admin trước khi rời nhóm.");
            }
        }    
        member.LeftAt = DateTime.Now;
        await _db.SaveChangesAsync();     
    }

    public async Task TransferAdminAsync(int conversationId, Guid userId, TransferAdminRequest req)
    {
        await GetGroupOrThrowAsync(conversationId);
        await RequireAdminAsync(conversationId, userId);

        if (userId == req.NewAdminId)
            throw AppException.BadRequest("Bạn đã là admin rồi.");

        var newAdmin = await _db.ConversationMembers
            .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId
                                    && cm.UserId == req.NewAdminId
                                    && cm.LeftAt ==null);   
        if (newAdmin is null)
            throw AppException.NotFound("Người dùng không trong nhóm.");           

        var currentAdmin = await _db.ConversationMembers
            .FirstOrDefaultAsync(c => c.ConversationId == conversationId
                                    && c.UserId == userId);
        if (currentAdmin is not null)
            currentAdmin.Role = ConversationMemberRole.Member;

        newAdmin.Role = ConversationMemberRole.Admin;
        await _db.SaveChangesAsync();    

    }

    private async Task RequireAdminAsync(int conversationId , Guid userId)
    {
        var isAdmin = await _db.ConversationMembers
                .AnyAsync(cm => cm.ConversationId == conversationId
                    && cm.UserId == userId
                    &&cm.Role == ConversationMemberRole.Admin
                    &&cm.LeftAt == null);
        if (!isAdmin)
            throw AppException.Forbidden("Bạn không có quyền Admin trong nhóm này");
    }

    private async Task<Conversation> GetGroupOrThrowAsync(int conversationId)
    {
        var conversation = await _db.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && !c.IsDeleted);

        if (conversation is null)
            throw AppException.NotFound("Cuộc trò chuyện không tồn tại");

        if (conversation.Type != ConversationType.Group)
            throw AppException.BadRequest("Cuộc trò chuyện này không phải nhóm");

        return conversation;
    }

    private async Task<ConversationResponse> BuildResponseAsync(
        Conversation conversation, Guid currentUserId)
    {
        // Lấy members
        var members = await _db.ConversationMembers
            .Where(m => m.ConversationId == conversation.Id && m.LeftAt == null)
            .Include(m => m.User)
            .ToListAsync();

        var memberInfos = members.Select(m => new MemberInfo
        {
            UserId = m.UserId,
            DisplayName = m.User.DisplayName,
            AvatarUrl = m.User.AvatarUrl,
            Role = m.Role,
            IsOnline = m.User.IsOnline,
            LastSeenAt = m.User.LastSeenAt,
            NicknameInGroup = m.NicknameInGroup,
        }).ToList();

        // Lấy last message
        LastMessageInfo? lastMessage = null;
        if (conversation.LastMessageId.HasValue)
        {
            var msg = await _db.Messages
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.Id == conversation.LastMessageId);

            if (msg is not null)
                lastMessage = new LastMessageInfo
                {
                    Id = msg.Id,
                    Content = msg.DeletedForAll ? null : msg.Content,
                    Type = msg.Type,
                    SenderId = msg.SenderId,
                    SenderName = msg.Sender.DisplayName,
                    SentAt = msg.SentAt,
                };
        }

        // Đếm unread
        var member = members.FirstOrDefault(m => m.UserId == currentUserId);
        var unread = 0;
        if (member is not null)
        {
            unread = await _db.Messages
                .CountAsync(m => m.ConversationId == conversation.Id
                              && !m.DeletedForAll
                              && m.SentAt > (member.LastReadAt ?? DateTime.MinValue)
                              && m.SenderId != currentUserId);
        }

        return new ConversationResponse
        {
            Id = conversation.Id,
            Type = conversation.Type,
            GroupName = conversation.GroupName,
            GroupAvatarUrl = conversation.GroupAvatarUrl,
            LastActivityAt = conversation.LastActivityAt,
            LastMessage = lastMessage,
            Members = memberInfos,
            UnreadCount = unread,
        };
    }
}
