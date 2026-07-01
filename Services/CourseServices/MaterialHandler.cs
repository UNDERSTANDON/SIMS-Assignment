using SIMS_Assignment.Models.CourseRelatedModels;

namespace SIMS_Assignment.Services.CourseServices
{
    public class MaterialHandler
    {
        private readonly List<Material> _materials;
        public void AddMaterial(Material material)
        {
            _materials.Add(material);
        }

        public void EditMaterial(Material material, int i)
        {
            DeleteMaterial(material.Id, i);
            _materials.Insert(i, material);
        }

        public void DeleteMaterial(int materialId, int i)
        {
            var materialToRemove = _materials.FirstOrDefault(m => m.Id == materialId);
            if (materialToRemove != null)
            {
                _materials.Remove(materialToRemove);
            }
        }
    }
}
