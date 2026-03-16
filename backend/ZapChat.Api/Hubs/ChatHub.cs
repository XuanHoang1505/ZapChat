using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ZapChat.Api.Common.Helpers;
using ZapChat.Api.Data;
using ZapChat.Api.DTOs.Messages;
using ZapChat.Api.Enums;
using ZapChat.Api.Services.Interfaces;

namespace ZapChat.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMessageService _messageService;
    private readonly ConnectionManager _connectionManager;
    private readonly AppDbContext _db;

    public ChatHub(
        IMessageService messageService,
        ConnectionManager connectionManager,
        AppDbContext db)
    {
        _messageService    = messageService;
        _connectionManager = connectionManager;
        _db                = db;
    }

    // ── Lấy UserId từ JWT ─────────────────────────────
    private Guid GetUserId()
        => Guid.Parse(Context.User!
            .FindFirst(ClaimTypes.NameIdentifier)!.Value);

    // ══════════════════════════════════════════════════
    // Connect
    // ══════════════════════════════════════════════════
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        _connectionManager.Add(userId, Context.ConnectionId);

        // Set online
        var user = await _db.Users.FindAsync(userId);
        if (user is not null)
        {
            user.IsOnline  = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        // Re-join tất cả conversation (Groups reset khi restart)
        var conversationIds = await _db.ConversationMembers
            .Where(m => m.UserId == userId && m.LeftAt == null)
            .Select(m => m.ConversationId)
            .ToListAsync();

        foreach (var convId in conversationIds)
            await Groups.AddToGroupAsync(Context.ConnectionId, convId.ToString());

        // Thông báo bạn bè online
        var friendIds = await GetFriendIdsAsync(userId);
        foreach (var friendId in friendIds)
            foreach (var connId in _connectionManager.GetConnections(friendId))
                await Clients.Client(connId).SendAsync("UserOnline", userId);

        await base.OnConnectedAsync();
    }

    // ══════════════════════════════════════════════════
    // Disconnect
    // ══════════════════════════════════════════════════
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        _connectionManager.Remove(userId, Context.ConnectionId);

        // Chỉ offline khi không còn connection nào (đóng hết tab)
        if (!_connectionManager.IsOnline(userId))
        {
            var user = await _db.Users.FindAsync(userId);
            if (user is not null)
            {
                user.IsOnline   = false;
                user.LastSeenAt = DateTime.UtcNow;
                user.UpdatedAt  = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            // Thông báo bạn bè offline
            var friendIds = await GetFriendIdsAsync(userId);
            foreach (var friendId in friendIds)
                foreach (var connId in _connectionManager.GetConnections(friendId))
                    await Clients.Client(connId).SendAsync("UserOffline", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ══════════════════════════════════════════════════
    // Gửi tin nhắn
    // ══════════════════════════════════════════════════
    public async Task SendMessage(SendMessageRequest req)
    {
        var senderId = GetUserId();

        var message = await _messageService.SaveMessageAsync(senderId, req);

        // Broadcast toàn bộ conversation
        await Clients
            .Group(req.ConversationId.ToString())
            .SendAsync("ReceiveMessage", message);
    }

    // ══════════════════════════════════════════════════
    // Typing
    // ══════════════════════════════════════════════════
    public async Task TypingStart(int conversationId)
    {
        var userId = GetUserId();
        var user   = await _db.Users.FindAsync(userId);

        // GroupExcept → không gửi lại cho chính mình
        await Clients
            .GroupExcept(conversationId.ToString(), Context.ConnectionId)
            .SendAsync("UserTyping", new {
                ConversationId = conversationId,
                UserId         = userId,
                DisplayName    = user?.DisplayName,
            });
    }

    public async Task TypingStop(int conversationId)
    {
        var userId = GetUserId();

        await Clients
            .GroupExcept(conversationId.ToString(), Context.ConnectionId)
            .SendAsync("UserStopTyping", new {
                ConversationId = conversationId,
                UserId         = userId,
            });
    }

    // ══════════════════════════════════════════════════
    // Read receipt
    // ══════════════════════════════════════════════════
    public async Task MarkAsRead(int messageId)
    {
        var userId = GetUserId();
        var marked = await _messageService.MarkAsReadAsync(messageId, userId);

        if (!marked) return;

        var message = await _db.Messages.FindAsync(messageId);
        if (message is null) return;

        await Clients
            .Group(message.ConversationId.ToString())
            .SendAsync("MessageRead", new {
                MessageId      = messageId,
                ConversationId = message.ConversationId,
                UserId         = userId,
                ReadAt         = DateTime.UtcNow,
            });
    }

    // ══════════════════════════════════════════════════
    // Reaction
    // ══════════════════════════════════════════════════
    public async Task ReactToMessage(int messageId, string emoji)
    {
        var userId   = GetUserId();
        var reaction = await _messageService
            .ReactToMessageAsync(messageId, userId, emoji);

        var message = await _db.Messages.FindAsync(messageId);
        if (message is null) return;

        await Clients
            .Group(message.ConversationId.ToString())
            .SendAsync("MessageReacted", new {
                MessageId      = messageId,
                ConversationId = message.ConversationId,
                Reaction       = reaction,
            });
    }

    public async Task RemoveReaction(int messageId, string emoji)
    {
        var userId  = GetUserId();
        var removed = await _messageService
            .RemoveReactionAsync(messageId, userId, emoji);

        if (!removed) return;

        var message = await _db.Messages.FindAsync(messageId);
        if (message is null) return;

        await Clients
            .Group(message.ConversationId.ToString())
            .SendAsync("ReactionRemoved", new {
                MessageId      = messageId,
                ConversationId = message.ConversationId,
                UserId         = userId,
                Emoji          = emoji,
            });
    }

    private async Task<List<Guid>> GetFriendIdsAsync(Guid userId)
        => await _db.Friends
            .Where(f => f.Status == FriendStatus.Accepted &&
                       (f.RequesterId == userId || f.AddresseeId == userId))
            .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId)
            .ToListAsync();
}