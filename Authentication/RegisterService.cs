using SIMS_Assignment.Abstract;
using SIMS_Assignment.Models;
using SIMS_Assignment.SecurityHasher;

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
