using SIMS_Assignment.Abstract;
using SIMS_Assignment.Authentication;
using SIMS_Assignment.Models;

namespace SIMS_Assignment.Services
{
    public class StudentService : UserService
    {
        protected readonly Student student;
        public StudentService(IAuth authService, IDataStorage storage)
        : base(authService, storage) { }

        // Register method
        public async Task<bool> RegisterAsync(Student student, string password)
        {
            return await _authService.RegisterAsync(student, password);
        }

        // Register course method
        public async Task<bool> RegisterCourseAsync(Student student, Course course)
        {
            // Logic implement later
            return await _storage.SaveUserAsync(student);
        }

        // View Dashboard method
        public override void ViewDashboard(User user)
        {
            // Logic implement later
            Console.WriteLine($"Student Dashboard for {student.Name}");
        }
    }
}
