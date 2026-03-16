using System.ComponentModel.DataAnnotations;

namespace ZapChat.Api.DTOs.Friends;

public class SendFriendRequest
{
    [Required(ErrorMessage = "UserId không được để trống.")]
    public Guid AddresseeId { get; set; }
}