using System.ComponentModel.DataAnnotations;

namespace ZapChat.Api.DTOs.Conversations;

public class UpdateGroupRequest
{
    [MaxLength(100, ErrorMessage = "Tên nhóm tối đa 100 ký tự.")]
    public string? GroupName { get; set; }
    public IFormFile? GroupAvatar { get; set; }
}