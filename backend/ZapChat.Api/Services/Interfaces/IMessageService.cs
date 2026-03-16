using ZapChat.Api.DTOs.Messages;
using ZapChat.Api.DTOs.Pages;

namespace ZapChat.Api.Services.Interfaces;

public interface IMessageService
{
    Task<MessageResponse> SaveMessageAsync(Guid senderId, SendMessageRequest req);
    Task<bool> MarkAsReadAsync(int messageId, Guid userId);
    Task<MessageResponse?> GetMessageAsync(int messageId);
    Task<ReactionResponse> ReactToMessageAsync(int messageId, Guid userId, string emoji);
    Task<bool> RemoveReactionAsync(int messageId, Guid userId, string emoji);
    Task<List<Guid>> GetConversationMemberIdsAsync(int conversationId);
    Task<PagedResult<MessageResponse>> GetMessagesAsync(
        int conversationId, Guid userId, int page, int limit);
    Task<MessageResponse> EditMessageAsync(int messageId, Guid userId, string content);
    Task DeleteMessageAsync(int messageId, Guid userId, bool forAll);
}