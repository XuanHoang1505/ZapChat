using System.ComponentModel.DataAnnotations;

namespace ZapChat.Api.DTOs.Conversations;

public class CreateGroupConversationRequest
{
    [Required(ErrorMessage = "Tên nhóm không được để trống.")]
    [MaxLength(100, ErrorMessage = "Tên nhóm tối đa 100 ký tự.")]
    public string GroupName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Danh sách thành viên không được để trống.")]
    [MinLength(2, ErrorMessage = "Nhóm cần ít nhất 2 thành viên.")]
    public List<Guid> MemberIds { get; set; } = [];
}