using System.ComponentModel.DataAnnotations;

namespace ZapChat.Api.DTOs.Auth;
public class RegisterRequest
{
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [MaxLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
    public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Tên hiển thị không được để trống.")]
    [MinLength(2, ErrorMessage = "Tên hiển thị tối thiểu 2 ký tự.")]
    [MaxLength(100, ErrorMessage = "Tên hiển thị tối đa 100 ký tự.")]
    public string FullName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự.")]
    [MaxLength(50, ErrorMessage = "Mật khẩu tối đa 50 ký tự.")]
    public string Password { get; set; } = string.Empty;
    [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống.")]
    [Compare("Password", ErrorMessage = "Xác nhận mật khẩu không khớp.")]
    public string PasswordConfirm { get; set; } = string.Empty;
}