namespace SIMS_Assignment.Models
{
    public class Course
    {
        public string CourseId { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public int Credits { get; set; }
        public int LecturerId { get; set; }
        public List<int> EnrolledStudentIds { get; set; } = new();
    }
}
