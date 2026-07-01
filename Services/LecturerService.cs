using SIMS_Assignment.Abstract;
using SIMS_Assignment.Authentication;
using SIMS_Assignment.Models;

namespace SIMS_Assignment.Services
{
    public class LecturerService : UserService
    {
        protected readonly Lecturer lecturer;
        public LecturerService(IAuth authService, IDataStorage storage)
        : base(authService, storage) { }

        public async Task GradeStudentAsync(Student student, Course course, bool isPassed)
        {
            // Logic to grade
        }

        public override void ViewDashboard(User user)
        {
            // Display Lecturer Dashboard
        }
    }
}
