namespace SIMS_Assignment.Models
{
    public class Lecturer : User
    {
        public string Specialization { get; set; } = string.Empty;
        public List<string> AssignedCourses { get; set; } = new List<string>();
    }
}
