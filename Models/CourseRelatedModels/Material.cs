namespace SIMS_Assignment.Models.CourseRelatedModels
{
    public class Material
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
        public string CourseId { get; set; } = string.Empty;
    }
}
