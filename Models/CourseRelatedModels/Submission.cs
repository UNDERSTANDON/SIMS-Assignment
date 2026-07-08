namespace SIMS_Assignment.Models.CourseRelatedModels
{
    public class Submission
    {
        public string Id { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string AssignmentTitle { get; set; } = string.Empty;
        public DateTime SubmissionDate { get; set; }
        public string Content { get; set; } = string.Empty;
        // Optional uploaded file path relative to content root
        public string? FilePath { get; set; }
        public string? OriginalFileName { get; set; }
        // Grading
        public double? Grade { get; set; }
        public string? GradedBy { get; set; }
        public DateTime? GradedAt { get; set; }
        public string? Feedback { get; set; }
    }
}
