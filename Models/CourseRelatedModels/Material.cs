namespace SIMS_Assignment.Models.CourseRelatedModels
{
    public class Material
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
        public int CourseId { get; set; }
        public Course? Course { get; set; }
    }
}
