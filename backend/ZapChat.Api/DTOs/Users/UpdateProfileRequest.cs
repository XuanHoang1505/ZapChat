using System.ComponentModel.DataAnnotations;

namespace ZapChat.Api.DTOs.Users;

public class UpdateProfileRequest
{
    [MaxLength(100, ErrorMessage = "Tên hiển thị tối đa 100 ký tự.")]
    public string? DisplayName { get; set; }

    [MaxLength(200, ErrorMessage = "Bio tối đa 200 ký tự.")]
    public string? Bio { get; set; }

    public IFormFile? Avatar { get; set; }
}