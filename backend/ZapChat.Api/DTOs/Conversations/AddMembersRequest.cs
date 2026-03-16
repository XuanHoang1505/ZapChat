using System.ComponentModel.DataAnnotations;

namespace ZapChat.Api.DTOs.Conversations;

public class AddMembersRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "Cần ít nhất 1 thành viên.")]
    public List<Guid> UserIds { get; set; } = [];
}