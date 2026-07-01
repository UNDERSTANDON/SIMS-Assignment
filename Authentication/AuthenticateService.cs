using SIMS_Assignment.Abstract;
using SIMS_Assignment.SecurityHasher;
using SIMS_Assignment.Models;

namespace SIMS_Assignment.Authentication
{
    public abstract class AuthenticateService
    {
        public enum AuthAction // Changed from private to public to fix CS0051
        {
            Login,
            Register
        }

        protected readonly IDataStorage _storage;
        protected readonly IPasswordHasher _hasher;

        protected AuthenticateService(IDataStorage storage, IPasswordHasher hasher)
        {
            _storage = storage;
            _hasher = hasher;
        }

        // Call AuthFacade
        protected async Task<bool> AuthenticateAsync(string username, string password, AuthAction action)
        {
            switch (action)
            {
                case AuthAction.Login:
                    var user = await _storage.GetUserByNameAsync(username);
                    if (user == null) return false;
                    return _hasher.Verify(password, user.PasswordHash);
                case AuthAction.Register:
                    var newUser = new User();
                    newUser.PasswordHash = _hasher.Hash(password);
                    return await _storage.SaveUserAsync(newUser);
                default:
                    throw new InvalidOperationException("Invalid authentication action.");
            }
        }
    }
}
