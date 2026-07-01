using SIMS_Assignment.Abstract;
using SIMS_Assignment.Models;
using SIMS_Assignment.Authentication.SecurityHasher;

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
                    // Since user is the factory method, we must have to default to student user for now. In the future, we can add a parameter to specify the user type.
                    var newUser = new Student();
                    newUser.PasswordHash = _hasher.Hash(password);
                    return await _storage.SaveUserAsync(newUser);
                default:
                    throw new InvalidOperationException("Invalid authentication action.");
            }
        }
    }
}
