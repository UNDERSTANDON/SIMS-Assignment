using SIMS_Assignment.Abstract;
using SIMS_Assignment.Authentication.SecurityHasher;
using SIMS_Assignment.Models;

namespace SIMS_Assignment.Authentication
{
    public class RegisterService : AuthenticateService
    {
        public RegisterService(IDataStorage storage, IPasswordHasher hasher) : base(storage, hasher) { }

        public async Task<bool> RegisterAsync(User user, string password)
        {
            user.PasswordHash = _hasher.Hash(password);
            return await _storage.SaveUserAsync(user);
        }
    }
}
