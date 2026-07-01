using SIMS_Assignment.Abstract;
using SIMS_Assignment.Authentication.SecurityHasher;

namespace SIMS_Assignment.Authentication
{
    public class LoginService : AuthenticateService
    {
        public LoginService(IDataStorage storage, IPasswordHasher hasher) : base(storage, hasher) { }

        public async Task<bool> LoginAsync(string username, string password)
        {
            var user = await _storage.GetUserByNameAsync(username);
            if (user == null) return false;

            return _hasher.Verify(password, user.PasswordHash);
        }
    }
}
