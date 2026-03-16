using System.ComponentModel.DataAnnotations;

namespace ZapChat.Api.DTOs.Conversations;

public class TransferAdminRequest
{
    [Required(ErrorMessage = "UserId không được để trống.")]
    public Guid NewAdminId { get; set; }
}