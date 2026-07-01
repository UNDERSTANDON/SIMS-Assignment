using SIMS_Assignment.Models;

namespace SIMS_Assignment.Authentication
{
    public interface IAuth
    {
        Task<bool> LoginAsync(string username, string password);
        Task<bool> RegisterAsync(User user, string password);
    }
}
