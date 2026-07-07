using SIMS_Assignment.Abstract;
using SIMS_Assignment.Authentication.SecurityHasher;

namespace SIMS_Assignment.Authentication
{
    public class LoginService(IDataStorage storage, IPasswordHasher hasher) : AuthenticateService(storage, hasher)
    {
        public async Task<bool> LoginAsync(string username, string password)
        => await AuthenticateAsync(username, password, AuthAction.Login);
    }
}
