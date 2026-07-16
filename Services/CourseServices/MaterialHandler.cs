using SIMS_Assignment.Models.CourseRelatedModels;
using SIMS_Assignment.Abstract;

namespace SIMS_Assignment.Services.CourseServices
{
    public class MaterialHandler
    {
        // Basic CRUD for material
        private readonly List<Material> _materials;
        private readonly IMaterialStorage _storage;
        private readonly object _fileLock = new();

        public MaterialHandler(IMaterialStorage storage)
        {
            _storage = storage;
            _materials = _storage.GetAllMaterialsAsync().GetAwaiter().GetResult();
        }

        public void AddMaterial(Material material)
        {
            lock (_fileLock)
            {
                _materials.Add(material);
                _storage.SaveMaterialAsync(material).GetAwaiter().GetResult();
            }
        }

        public void EditMaterial(Material material)
        {
            lock (_fileLock)
            {
                var materialToRemove = _materials.FirstOrDefault(m => m.Id == material.Id);
                if (materialToRemove != null)
                {
                    _materials.Remove(materialToRemove);
                }
                _materials.Add(material);
                _storage.SaveMaterialAsync(material).GetAwaiter().GetResult();
            }
        }

        public void DeleteMaterial(string materialId)
        {
            lock (_fileLock)
            {
                var materialToRemove = _materials.FirstOrDefault(m => m.Id == materialId);
                if (materialToRemove != null)
                {
                    _materials.Remove(materialToRemove);
                    _storage.DeleteMaterialAsync(materialId).GetAwaiter().GetResult();
                }
            }
        }

        // Read access
        public List<Material> GetAll()
        {
            lock (_fileLock)
            {
                return _materials.ToList();
            }
        }

        // File mapping methods
        public void SaveFileMapping(
            string materialId,
            string originalFileName,
            string hashedFileName,
            string courseId
        )
        {
            try
            {
                lock (_fileLock)
                {
                    var mapping = new FileMapping
                    {
                        MaterialId = materialId,
                        OriginalFileName = originalFileName,
                        HashedFileName = hashedFileName,
                        CourseId = courseId,
                        UploadDate = DateTime.Now,
                    };
                    _storage.SaveFileMappingAsync(mapping).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving file mapping: {ex.Message}");
                throw; // Re-throw the exception to be handled by the caller
            }
        }

        public string? GetOriginalFileName(string materialId)
        {
            try
            {
                var mappings = _storage.GetFileMappingsAsync().GetAwaiter().GetResult();
                return mappings.FirstOrDefault(m => m.MaterialId == materialId)?.OriginalFileName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving file mapping: {ex.Message}");
                return null;
            }
        }

        public FileMapping? GetFileMapping(string materialId)
        {
            try
            {
                var mappings = _storage.GetFileMappingsAsync().GetAwaiter().GetResult();
                return mappings.FirstOrDefault(m => m.MaterialId == materialId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving file mapping: {ex.Message}");
                return null;
            }
        }
    }
}
