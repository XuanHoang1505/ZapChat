using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using ZapChat.Api.Common.Response;
using ZapChat.Api.DTOs.Friends;
using ZapChat.Api.Enums;
using ZapChat.Api.Hubs;
using ZapChat.Api.Services.Interfaces;

namespace ZapChat.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FriendController : ControllerBase
{
    private readonly IFriendService _friendService;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly Common.Helpers.ConnectionManager _connectionManager;

    public FriendController(
        IFriendService friendService,
        IHubContext<ChatHub> hubContext,
        Common.Helpers.ConnectionManager connectionManager)
    {
        _friendService     = friendService;
        _hubContext        = hubContext;
        _connectionManager = connectionManager;
    }

    private Guid GetUserId()
        => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    // GET /api/friend
    [HttpGet]
    public async Task<IActionResult> GetFriends()
    {
        var result = await _friendService.GetFriendsAsync(GetUserId());
        return Ok(ApiResponse<List<FriendResponse>>.Ok(result));
    }

    // GET /api/friend/pending
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var result = await _friendService.GetPendingRequestsAsync(GetUserId());
        return Ok(ApiResponse<List<FriendResponse>>.Ok(result));
    }

    // GET /api/friend/sent
    [HttpGet("sent")]
    public async Task<IActionResult> GetSent()
    {
        var result = await _friendService.GetSentRequestsAsync(GetUserId());
        return Ok(ApiResponse<List<FriendResponse>>.Ok(result));
    }

    // POST /api/friend/request
    [HttpPost("request")]
    public async Task<IActionResult> SendRequest(SendFriendRequest req)
    {
        var userId = GetUserId();
        var result = await _friendService.SendRequestAsync(userId, req.AddresseeId);

        // Notify realtime cho người nhận
        foreach (var connId in _connectionManager.GetConnections(req.AddresseeId))
            await _hubContext.Clients
                .Client(connId)
                .SendAsync("FriendRequestReceived", result);

        return Ok(ApiResponse<FriendResponse>.Ok(result, "Đã gửi lời mời kết bạn."));
    }

    // PUT /api/friend/{id}/accept
    [HttpPut("{id:int}/accept")]
    public async Task<IActionResult> Accept(int id)
    {
        var userId = GetUserId();
        var result = await _friendService.AcceptRequestAsync(userId, id);

        // Notify realtime cho người gửi lời mời
        foreach (var connId in _connectionManager.GetConnections(result.UserId))
            await _hubContext.Clients
                .Client(connId)
                .SendAsync("FriendRequestAccepted", result);

        return Ok(ApiResponse<FriendResponse>.Ok(result, "Đã chấp nhận lời mời kết bạn."));
    }

    // DELETE /api/friend/{id}/decline
    [HttpDelete("{id:int}/decline")]
    public async Task<IActionResult> Decline(int id)
    {
        await _friendService.DeclineRequestAsync(GetUserId(), id);
        return Ok(ApiResponse<object>.Ok(null, "Đã từ chối lời mời kết bạn."));
    }

    // DELETE /api/friend/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Unfriend(int id)
    {
        await _friendService.UnfriendAsync(GetUserId(), id);
        return Ok(ApiResponse<object>.Ok(null, "Đã huỷ kết bạn."));
    }

    // POST /api/friend/{id}/block
    [HttpPost("{id:int}/block")]
    public async Task<IActionResult> Block(int id)
    {
        await _friendService.BlockAsync(GetUserId(), id);
        return Ok(ApiResponse<object>.Ok(null, "Đã chặn người dùng."));
    }

    // DELETE /api/friend/{id}/unblock
    [HttpDelete("{id:int}/unblock")]
    public async Task<IActionResult> Unblock(int id)
    {
        await _friendService.UnblockAsync(GetUserId(), id);
        return Ok(ApiResponse<object>.Ok(null, "Đã bỏ chặn người dùng."));
    }

    // GET /api/friend/relationship/{targetUserId}
    [HttpGet("relationship/{targetUserId:guid}")]
    public async Task<IActionResult> GetRelationship(Guid targetUserId)
    {
        var status = await _friendService.GetRelationshipAsync(GetUserId(), targetUserId);
        return Ok(ApiResponse<object>.Ok(new { Status = status ?? FriendStatus.None }));
    }
}