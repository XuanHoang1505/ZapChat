namespace ZapChat.Api.Repositories.Interfaces;

public interface IUserRepository
{
    Task<bool> IsUserExists(string email);
}