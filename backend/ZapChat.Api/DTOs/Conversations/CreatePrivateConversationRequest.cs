using System.ComponentModel.DataAnnotations;

namespace ZapChat.Api.DTOs.Conversations;

public class CreatePrivateConversationRequest
{
    [Required(ErrorMessage = "UserId không được để trống.")]
    public Guid TargetUserId { get; set; }
}