namespace SIMS_Assignment.Models
{
    public class Course
    {
        public string CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int Credits { get; set; }
        public int LecturerId { get; set; } // Links to the Lecturer teaching the course
        public List<int> EnrolledStudentIds { get; set; } = new List<int>();
    }
}
