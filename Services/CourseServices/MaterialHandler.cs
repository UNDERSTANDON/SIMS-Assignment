using SIMS_Assignment.Models.CourseRelatedModels;
using Microsoft.AspNetCore.Hosting;
using SIMS_Assignment.Storage;

namespace SIMS_Assignment.Services.CourseServices
{
    public class MaterialHandler
    {
        // Basic CRUD for material
        private readonly List<Material> _materials;
        private readonly JsonStorage<Material> _materialStorage;
        private readonly JsonStorage<FileMapping> _fileMappingStorage;
        private readonly object _fileLock = new();

        public MaterialHandler(IWebHostEnvironment env)
        {
            _materialStorage = new JsonStorage<Material>(env, "materials.json");
            _fileMappingStorage = new JsonStorage<FileMapping>(env, "file_mappings.json");
            _materials = _materialStorage.Load();
        }

        public void AddMaterial(Material material)
        {
            lock (_fileLock)
            {
                _materials.Add(material);
                _materialStorage.Save(_materials);
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
                _materialStorage.Save(_materials);
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
                    _materialStorage.Save(_materials);
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
                    var mappings = _fileMappingStorage.Load();
                    mappings.Add(
                        new FileMapping
                        {
                            MaterialId = materialId,
                            OriginalFileName = originalFileName,
                            HashedFileName = hashedFileName,
                            CourseId = courseId,
                            UploadDate = DateTime.Now,
                        }
                    );
                    _fileMappingStorage.Save(mappings);
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
                var mappings = _fileMappingStorage.Load();
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
                var mappings = _fileMappingStorage.Load();
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
