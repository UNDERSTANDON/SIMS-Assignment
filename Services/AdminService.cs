using SIMS_Assignment.Abstract;
using SIMS_Assignment.Authentication;
using SIMS_Assignment.Models;

namespace SIMS_Assignment.Services
{
    public class AdminService : UserService
    {
        public AdminService(IAuth authService, IDataStorage storage)
        : base(authService, storage) { }

        public async Task CreateCourseAsync(Course newCourse)
        {
            // Logic to create course
        }

        public override void ViewDashboard(User user)
        {
            // Display Admin Dashboard
        }
    }
}
