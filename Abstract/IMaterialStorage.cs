using System.Collections.Generic;
using System.Threading.Tasks;
using SIMS_Assignment.Models.CourseRelatedModels;
using SIMS_Assignment.Services.CourseServices;

namespace SIMS_Assignment.Abstract
{
    public interface IMaterialStorage
    {
        Task<List<Material>> GetAllMaterialsAsync();
        Task<bool> SaveMaterialAsync(Material material);
        Task<bool> DeleteMaterialAsync(string id);
        Task<List<FileMapping>> GetFileMappingsAsync();
        Task<bool> SaveFileMappingAsync(FileMapping mapping);
    }
}
