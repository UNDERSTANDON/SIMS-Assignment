using System.Text.Json;
using SIMS_Assignment.Models.CourseRelatedModels;

namespace SIMS_Assignment.Services.CourseServices
{
    public class MaterialHandler
    {
        // Basic CRUD for material
        private readonly List<Material> _materials = new();
        private readonly string _fileMappingPath;
        private readonly string _storagePath;
        private readonly object _fileLock = new();

        public MaterialHandler(IWebHostEnvironment env)
        {
            var dataDir = Path.Combine(env.ContentRootPath, "DataStorage");
            if (!Directory.Exists(dataDir))
                Directory.CreateDirectory(dataDir);
            _fileMappingPath = Path.Combine(dataDir, "file_mappings.json");
            _storagePath = Path.Combine(dataDir, "materials.json");
            LoadFromDisk();
        }

        public void AddMaterial(Material material)
        {
            _materials.Add(material);
            SaveToDisk();
        }

        public void EditMaterial(Material material)
        {
            DeleteMaterial(material.Id);
            lock (_fileLock)
            {
                _materials.Add(material);
                SaveToDisk();
            }
        }

        public void DeleteMaterial(string materialId)
        {
            var materialToRemove = _materials.FirstOrDefault(m => m.Id == materialId);
            if (materialToRemove != null)
            {
                _materials.Remove(materialToRemove);
                SaveToDisk();
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
                    var mappings = LoadFileMappings();
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
                    SaveFileMappingsToJson(mappings);
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
                var mappings = LoadFileMappings();
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
                var mappings = LoadFileMappings();
                return mappings.FirstOrDefault(m => m.MaterialId == materialId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving file mapping: {ex.Message}");
                return null;
            }
        }

        private List<FileMapping> LoadFileMappings()
        {
            if (!File.Exists(_fileMappingPath))
                return new List<FileMapping>();

            try
            {
                var json = File.ReadAllText(_fileMappingPath);
                return JsonSerializer.Deserialize<List<FileMapping>>(json)
                    ?? new List<FileMapping>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to load file mappings; refusing to overwrite existing mappings.", ex);
            }
        }

        private void SaveFileMappingsToJson(List<FileMapping> mappings)
        {
            try
            {
                var directory = Path.GetDirectoryName(_fileMappingPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(mappings, options);
                File.WriteAllText(_fileMappingPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving file mappings to JSON: {ex.Message}");
            }
        }

        private void SaveToDisk()
        {
            try
            {
                lock (_fileLock)
                {
                    var opts = new JsonSerializerOptions { WriteIndented = true };
                    var json = JsonSerializer.Serialize(_materials, opts);
                    File.WriteAllText(_storagePath, json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving materials to disk: {ex.Message}");
            }
        }

        private void LoadFromDisk()
        {
            try
            {
                lock (_fileLock)
                {
                    if (!File.Exists(_storagePath))
                        return;
                    var json = File.ReadAllText(_storagePath);
                    var list = JsonSerializer.Deserialize<List<Material>>(json);
                    if (list != null)
                    {
                        _materials.Clear();
                        _materials.AddRange(list);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading materials from disk: {ex.Message}");
            }
        }
    }
}
