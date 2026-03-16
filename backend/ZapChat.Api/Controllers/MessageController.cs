using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using ZapChat.Api.Common.Exceptions;
using ZapChat.Api.Common.Response;
using ZapChat.Api.DTOs.Messages;
using ZapChat.Api.DTOs.Pages;
using ZapChat.Api.Hubs;
using ZapChat.Api.Services.Interfaces;

namespace ZapChat.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly IHubContext<ChatHub> _hubContext;

    public MessageController(
        IMessageService messageService,
        IHubContext<ChatHub> hubContext)
    {
        _messageService = messageService;
        _hubContext     = hubContext;
    }

    private Guid GetUserId()
        => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet("{conversationId:int}")]
    public async Task<IActionResult> GetMessages(
        int conversationId,
        [FromQuery] int page  = 1,
        [FromQuery] int limit = 50)
    {
        var result = await _messageService
            .GetMessagesAsync(conversationId, GetUserId(), page, limit);

        return Ok(ApiResponse<PagedResult<MessageResponse>>.Ok(result));
    }

    // PUT /api/message/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> EditMessage(
        int id, [FromBody] EditMessageRequest req)
    {
        var response = await _messageService
            .EditMessageAsync(id, GetUserId(), req.Content);

        // Broadcast — toàn bộ conversation thấy tin nhắn đã sửa
        await _hubContext.Clients
            .Group(response.ConversationId.ToString())
            .SendAsync("MessageEdited", response);

        return Ok(ApiResponse<MessageResponse>.Ok(response, "Sửa tin nhắn thành công."));
    }

    // DELETE /api/message/{id}?forAll=true
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteMessage(
        int id, [FromQuery] bool forAll = false)
    {
        var userId  = GetUserId();
        var message = await _messageService.GetMessageAsync(id);

        if (message is null)
            throw AppException.NotFound(
                "Tin nhắn không tồn tại.");

        await _messageService.DeleteMessageAsync(id, userId, forAll);

        if (forAll)
        {
            // Broadcast — toàn bộ conversation thấy tin nhắn đã xoá
            await _hubContext.Clients
                .Group(message.ConversationId.ToString())
                .SendAsync("MessageDeleted", new {
                    MessageId      = id,
                    ConversationId = message.ConversationId,
                    DeletedForAll  = true,
                });
        }

        return Ok(ApiResponse<object>.Ok(null, "Xoá tin nhắn thành công."));
    }
}
