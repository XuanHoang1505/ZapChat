namespace ZapChat.Api.DTOs.Messages;

public class ReactionResponse
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Emoji { get; set; }    = string.Empty;
}