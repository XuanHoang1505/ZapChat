using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ZapChat.Api.Common.Response;
using ZapChat.Api.DTOs.Conversations;
using ZapChat.Api.Hubs;
using ZapChat.Api.Services.Interfaces;

namespace ZapChat.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/conversations")]
public class ConversationController : ControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly IHubContext<ChatHub> _hubContext;
    public ConversationController(
        IConversationService conversationService,
        IHubContext<ChatHub> hubContext)
    {
        _conversationService = conversationService;
        _hubContext          = hubContext;
    }
    private Guid GetUserId()
        => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet]
    public async Task<IActionResult> GetMyConversations()
    {
        var result = await _conversationService.GetMyConversationsAsync(GetUserId());
        return Ok(ApiResponse<List<ConversationResponse>>.Ok(result));
    }
    // GET /api/conversation/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _conversationService.GetByIdAsync(id, GetUserId());
        return Ok(ApiResponse<ConversationResponse>.Ok(result));
    }

    // POST /api/conversation/private
    [HttpPost("private")]
    public async Task<IActionResult> CreatePrivate(CreatePrivateConversationRequest req)
    {
        var result = await _conversationService.CreatePrivateAsync(GetUserId(), req);

        // Notify người được tạo conversation cùng
        await _hubContext.Clients
            .Group(result.Id.ToString())
            .SendAsync("ConversationCreated", result);

        return Ok(ApiResponse<ConversationResponse>.Ok(
            result, "Tạo cuộc trò chuyện thành công."));
    }

    // POST /api/conversation/group
    [HttpPost("group")]
    public async Task<IActionResult> CreateGroup(CreateGroupConversationRequest req)
    {
        var result = await _conversationService.CreateGroupAsync(GetUserId(), req);

        // Notify tất cả member
        await _hubContext.Clients
            .Group(result.Id.ToString())
            .SendAsync("ConversationCreated", result);

        return Ok(ApiResponse<ConversationResponse>.Ok(
            result, "Tạo nhóm thành công."));
    }

    // PUT /api/conversation/{id}/group
    [HttpPut("{id:int}/group")]
    public async Task<IActionResult> UpdateGroup(int id, [FromForm] UpdateGroupRequest req)
    {
        var result = await _conversationService.UpdateGroupAsync(id, GetUserId(), req);

        await _hubContext.Clients
            .Group(id.ToString())
            .SendAsync("GroupUpdated", result);

        return Ok(ApiResponse<ConversationResponse>.Ok(
            result, "Cập nhật nhóm thành công."));
    }

    // POST /api/conversation/{id}/members
    [HttpPost("{id:int}/members")]
    public async Task<IActionResult> AddMembers(int id, AddMembersRequest req)
    {
        await _conversationService.AddMembersAsync(id, GetUserId(), req);

        var updated = await _conversationService.GetByIdAsync(id, GetUserId());

        // Notify nhóm có thành viên mới
        await _hubContext.Clients
            .Group(id.ToString())
            .SendAsync("MembersAdded", updated);

        return Ok(ApiResponse<ConversationResponse>.Ok(
            updated, "Thêm thành viên thành công."));
    }

    // DELETE /api/conversation/{id}/members/{userId}
    [HttpDelete("{id:int}/members/{targetUserId:guid}")]
    public async Task<IActionResult> RemoveMember(int id, Guid targetUserId)
    {
        await _conversationService.RemoveMemberAsync(id, GetUserId(), targetUserId);

        // Notify nhóm
        await _hubContext.Clients
            .Group(id.ToString())
            .SendAsync("MemberRemoved", new {
                ConversationId = id,
                UserId         = targetUserId,
            });

        return Ok(ApiResponse<object>.Ok(null, "Xoá thành viên thành công."));
    }

    // POST /api/conversation/{id}/leave
    [HttpPost("{id:int}/leave")]
    public async Task<IActionResult> Leave(int id)
    {
        var userId = GetUserId();
        await _conversationService.LeaveGroupAsync(id, userId);

        // Notify nhóm có người rời
        await _hubContext.Clients
            .Group(id.ToString())
            .SendAsync("MemberLeft", new {
                ConversationId = id,
                UserId         = userId,
            });

        return Ok(ApiResponse<object>.Ok(null, "Rời nhóm thành công."));
    }

    // PUT /api/conversation/{id}/transfer-admin
    [HttpPut("{id:int}/transfer-admin")]
    public async Task<IActionResult> TransferAdmin(int id, TransferAdminRequest req)
    {
        await _conversationService.TransferAdminAsync(id, GetUserId(), req);

        await _hubContext.Clients
            .Group(id.ToString())
            .SendAsync("AdminTransferred", new {
                ConversationId = id,
                NewAdminId     = req.NewAdminId,
            });

        return Ok(ApiResponse<object>.Ok(null, "Chuyển quyền admin thành công."));
    }
}