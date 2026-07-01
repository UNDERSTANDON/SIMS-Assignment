namespace SIMS_Assignment.Models.CourseRelatedModels
{
    public class Submission
    {
        public string StudentId { get; set; } = string.Empty;
        public string AssignmentTitle { get; set; } = string.Empty;
        public DateTime SubmissionDate { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
