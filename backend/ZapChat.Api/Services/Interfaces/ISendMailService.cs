namespace ZapChat.Api.Services.Interfaces
{
    public interface ISendMailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body);
    }
}