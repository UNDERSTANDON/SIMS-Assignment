namespace SIMS_Assignment.Models.CourseRelatedModels
{
    public class Assignment
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public List<Submission> Submissions { get; set; } = new List<Submission>();
    }
}
