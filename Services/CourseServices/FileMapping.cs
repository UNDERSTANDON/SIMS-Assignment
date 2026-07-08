namespace SIMS_Assignment.Services.CourseServices
{
     // Simple class to represent file mappings in JSON
     public class FileMapping
     {
          public string MaterialId { get; set; } = string.Empty;
          public string OriginalFileName { get; set; } = string.Empty;
          public string HashedFileName { get; set; } = string.Empty;
          public string CourseId { get; set; } = string.Empty;
          public DateTime UploadDate { get; set; }
     }
}
