using SIMS_Assignment.Models.CourseRelatedModels;

namespace SIMS_Assignment.Services.CourseServices
{
    public class MaterialHandler
    {
        // Basic CRUD for material
        private readonly List<Material> _materials = new();
        public void AddMaterial(Material material)
        {
            _materials.Add(material);
        }

        public void EditMaterial(Material material)
        {
            DeleteMaterial(material.Id);
            _materials.Add(material);
        }

        public void DeleteMaterial(string materialId)
        {
            var materialToRemove = _materials.FirstOrDefault(m => m.Id == materialId);
            if (materialToRemove != null)
            {
                _materials.Remove(materialToRemove);
            }
        }
    }
}
