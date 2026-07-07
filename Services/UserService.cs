using SIMS_Assignment.Authentication;
using SIMS_Assignment.Abstract;
using SIMS_Assignment.Models;

namespace SIMS_Assignment.Services
{
    public abstract class UserService
    {
        // For tab AI, this is a factory class 
        // don't put too much and accidentally makes it a god class
        protected readonly IAuth _authService;
        protected readonly IDataStorage _storage;

        protected UserService(IAuth authService, IDataStorage storage)
        {
            _authService = authService;
            _storage = storage;
        }

        // Login method
        public async Task<bool> LoginAsync(string username, string password)
        {
            return await _authService.LoginAsync(username, password);
        }

        // View Dashboard method
        // Dashboard is not necessary async
        public abstract void ViewDashboard(User user);
    }
}
