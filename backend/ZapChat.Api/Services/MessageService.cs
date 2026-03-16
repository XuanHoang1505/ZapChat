using Microsoft.EntityFrameworkCore;
using ZapChat.Api.Common.Exceptions;
using ZapChat.Api.Data;
using ZapChat.Api.DTOs.Messages;
using ZapChat.Api.DTOs.Pages;
using ZapChat.Api.Enums;
using ZapChat.Api.Models;
using ZapChat.Api.Services.Interfaces;

namespace ZapChat.Api.Services;

public class MessageService : IMessageService
{
    private readonly AppDbContext _db;

    public MessageService(AppDbContext db) => _db = db;

    public async Task<MessageResponse> SaveMessageAsync(
        Guid senderId, SendMessageRequest req)
    {
        var isMember = await _db.ConversationMembers
            .AnyAsync(m => m.ConversationId == req.ConversationId
                        && m.UserId == senderId
                        && m.LeftAt == null);

        if (!isMember)
            throw AppException.Forbidden(
                "Bạn không có quyền gửi tin nhắn vào cuộc trò chuyện này.");

        if (req.ReplyToId.HasValue)
        {
            var replyExists = await _db.Messages
                .AnyAsync(m => m.Id == req.ReplyToId.Value
                            && m.ConversationId == req.ConversationId
                            && !m.DeletedForAll);
            if (!replyExists)
                throw AppException.NotFound("Tin nhắn reply không tồn tại.");
        }

        var message = new Message
        {
            ConversationId = req.ConversationId,
            SenderId = senderId,
            Content = req.Content?.Trim(),
            Type = req.Type,
            ReplyToId = req.ReplyToId,
            SentAt = DateTime.UtcNow,
        };

        _db.Messages.Add(message);

        // Lưu attachments nếu có
        if (req.Attachments?.Count > 0)
        {
            foreach (var att in req.Attachments)
            {
                _db.MessageAttachments.Add(new MessageAttachment
                {
                    MessageId = message.Id,
                    FileUrl = att.FileUrl,
                    PublicId = att.PublicId,
                    FileName = att.FileName,
                    FileSize = att.FileSize,
                    MimeType = att.MimeType,
                    Width = att.Width,
                    Height = att.Height,
                });
            }
        }

        await _db.SaveChangesAsync();

        // Cập nhật LastMessage + LastActivityAt
        var conversation = await _db.Conversations.FindAsync(req.ConversationId);
        if (conversation is not null)
        {
            conversation.LastMessageId = message.Id;
            conversation.LastActivityAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return await BuildMessageResponseAsync(message);
    }

    public async Task<bool> MarkAsReadAsync(int messageId, Guid userId)
    {
        var alreadyRead = await _db.MessageReadReceipts
            .AnyAsync(r => r.MessageId == messageId && r.UserId == userId);

        if (alreadyRead) return false;

        var message = await _db.Messages.FindAsync(messageId);
        if (message is null) return false;

        // Không tự đánh dấu đọc tin nhắn của chính mình
        if (message.SenderId == userId) return false;

        _db.MessageReadReceipts.Add(new MessageReadReceipt
        {
            MessageId = messageId,
            UserId = userId,
            ReadAt = DateTime.Now,
        });

        // Cập nhật LastReadAt của member trong conversation
        var member = await _db.ConversationMembers
            .FirstOrDefaultAsync(m => m.ConversationId == message.ConversationId
                                   && m.UserId == userId);
        if (member is not null)
            member.LastReadAt = DateTime.Now;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<ReactionResponse> ReactToMessageAsync(
        int messageId, Guid userId, string emoji)
    {
        var messageExists = await _db.Messages
            .AnyAsync(m => m.Id == messageId && !m.DeletedForAll);
        if (!messageExists)
            throw AppException.NotFound("Tin nhắn không tồn tại.");

        var existing = await _db.MessageReactions
            .FirstOrDefaultAsync(r => r.MessageId == messageId
                                   && r.UserId == userId
                                   && r.Emoji == emoji);
        if (existing is not null)
            throw AppException.Conflict("Bạn đã react emoji này rồi.");

        _db.MessageReactions.Add(new MessageReaction
        {
            MessageId = messageId,
            UserId = userId,
            Emoji = emoji,
        });

        await _db.SaveChangesAsync();

        var user = await _db.Users.FindAsync(userId);
        return new ReactionResponse
        {
            UserId = userId,
            UserName = user?.DisplayName ?? "",
            Emoji = emoji,
        };
    }

    public async Task<bool> RemoveReactionAsync(
        int messageId, Guid userId, string emoji)
    {
        var reaction = await _db.MessageReactions
            .FirstOrDefaultAsync(r => r.MessageId == messageId
                                   && r.UserId == userId
                                   && r.Emoji == emoji);
        if (reaction is null) return false;

        _db.MessageReactions.Remove(reaction);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<MessageResponse?> GetMessageAsync(int messageId)
    {
        var message = await _db.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId && !m.DeletedForAll);
        return message is null ? null : await BuildMessageResponseAsync(message);
    }

    // ── Lấy danh sách memberId của conversation ───────
    public async Task<List<Guid>> GetConversationMemberIdsAsync(int conversationId)
        => await _db.ConversationMembers
            .Where(m => m.ConversationId == conversationId && m.LeftAt == null)
            .Select(m => m.UserId)
            .ToListAsync();

    // ── Build MessageResponse ─────────────────────────
    private async Task<MessageResponse> BuildMessageResponseAsync(Message message)
    {
        var sender = await _db.Users.FindAsync(message.SenderId);

        var attachments = await _db.MessageAttachments
            .Where(a => a.MessageId == message.Id)
            .Select(a => new AttachmentResponse
            {
                Id = a.Id,
                FileUrl = a.FileUrl,
                FileName = a.FileName,
                FileSize = a.FileSize,
                MimeType = a.MimeType,
                Width = a.Width,
                Height = a.Height,
            })
            .ToListAsync();

        var reactions = await _db.MessageReactions
            .Where(r => r.MessageId == message.Id)
            .Include(r => r.User)
            .Select(r => new ReactionResponse
            {
                UserId = r.UserId,
                UserName = r.User.DisplayName,
                Emoji = r.Emoji,
            })
            .ToListAsync();

        var readBy = await _db.MessageReadReceipts
            .Where(r => r.MessageId == message.Id)
            .Include(r => r.User)
            .Select(r => new ReadReceiptResponse
            {
                UserId = r.UserId,
                UserName = r.User.DisplayName,
                ReadAt = r.ReadAt,
            })
            .ToListAsync();

        return new MessageResponse
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            SenderName = sender?.DisplayName ?? "",
            SenderAvatar = sender?.AvatarUrl,
            Content = message.Content,
            Type = message.Type,
            ReplyToId = message.ReplyToId,
            IsEdited = message.IsEdited,
            SentAt = message.SentAt,
            Attachments = attachments,
            Reactions = reactions,
            ReadBy = readBy,
        };
    }

    public async Task<PagedResult<MessageResponse>> GetMessagesAsync(
    int conversationId, Guid userId, int page, int limit)
    {
        // Kiểm tra user có trong conversation không
        var isMember = await _db.ConversationMembers
            .AnyAsync(m => m.ConversationId == conversationId
                        && m.UserId == userId
                        && m.LeftAt == null);

        if (!isMember)
            throw AppException.Forbidden(
                "Bạn không có quyền xem cuộc trò chuyện này.");

        limit = Math.Clamp(limit, 1, 100);

        var total = await _db.Messages
            .CountAsync(m => m.ConversationId == conversationId
                          && !m.DeletedForAll);

        var messages = await _db.Messages
            .Where(m => m.ConversationId == conversationId && !m.DeletedForAll)
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        var result = new List<MessageResponse>();
        foreach (var msg in messages)
        {
            var response = await BuildMessageResponseAsync(msg);
            result.Add(response);
        }

        return new PagedResult<MessageResponse>
        {
            Data = result,
            Total = total,
            Page = page,
            Limit = limit,
            TotalPages = (int)Math.Ceiling((double)total / limit),
        };
    }

    // ── Sửa tin nhắn ─────────────────────────────────
    public async Task<MessageResponse> EditMessageAsync(
        int messageId, Guid userId, string content)
    {
        var message = await _db.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId && !m.DeletedForAll);

        if (message is null)
            throw AppException.NotFound("Tin nhắn không tồn tại.");

        if (message.SenderId != userId)
            throw AppException.Forbidden("Bạn không có quyền sửa tin nhắn này.");

        if (message.Type != MessageType.Text)
            throw AppException.BadRequest("Chỉ có thể sửa tin nhắn văn bản.");

        message.Content = content.Trim();
        message.IsEdited = true;
        message.EditedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        return await BuildMessageResponseAsync(message);
    }

    // ── Xoá tin nhắn ─────────────────────────────────
    public async Task DeleteMessageAsync(int messageId, Guid userId, bool forAll)
    {
        var message = await _db.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId && !m.DeletedForAll);

        if (message is null)
            throw AppException.NotFound("Tin nhắn không tồn tại.");

        if (forAll)
        {
            if (message.SenderId != userId)
                throw AppException.Forbidden(
                    "Chỉ người gửi mới có thể xoá tin nhắn với tất cả.");

            message.DeletedForAll = true;
            message.Content = null;
        }
        else
        {
            // Xoá chỉ phía mình
            message.IsDeleted = true;
        }

        await _db.SaveChangesAsync();
    }
}