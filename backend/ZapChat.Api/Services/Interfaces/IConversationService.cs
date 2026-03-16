using ZapChat.Api.DTOs.Conversations;

namespace ZapChat.Api.Services.Interfaces;

public interface IConversationService
{
    Task<ConversationResponse> CreatePrivateAsync(
        Guid userId, CreatePrivateConversationRequest req);
    Task<ConversationResponse> CreateGroupAsync(
        Guid userId, CreateGroupConversationRequest req);

    Task<List<ConversationResponse>> GetMyConversationsAsync(Guid userId);
    Task<ConversationResponse> GetByIdAsync(int conversationId, Guid userId);

    Task<ConversationResponse> UpdateGroupAsync(int conversationId, Guid userId, UpdateGroupRequest req);
    Task AddMembersAsync(int conversationId, Guid userId, AddMembersRequest req);
    Task RemoveMemberAsync(int conversationId, Guid adminId, Guid targetUserId);
    Task LeaveGroupAsync(int conversationId, Guid userId);
    Task TransferAdminAsync(int conversationId, Guid userId, TransferAdminRequest req);
}