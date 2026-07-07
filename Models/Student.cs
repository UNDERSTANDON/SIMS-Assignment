namespace SIMS_Assignment.Models
{
    public class Student : User
    {
        public string CurrentProgram { get; set; } = string.Empty;
        public Dictionary<string, bool> AcademicRecords { get; set; } = new();
        public List<Course> Courses { get; set; } = new();
    }
}
