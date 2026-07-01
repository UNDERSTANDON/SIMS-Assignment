using SIMS_Assignment.Models;
using SIMS_Assignment.Services;

namespace SIMS_Assignment.Authentication
{
    // The Facade: Wires it together so UserService only needs one dependency
    public class AuthFacade : IAuth
    {
        private readonly LoginService _loginService;
        private readonly RegisterService _registerService;

        public AuthFacade(LoginService loginService, RegisterService registerService)
        {
            _loginService = loginService;
            _registerService = registerService;
        }

        public Task<bool> LoginAsync(string username, string password)
            => _loginService.LoginAsync(username, password);

        public Task<bool> RegisterAsync(User user, string password)
            => _registerService.RegisterAsync(user, password);
    }
}
