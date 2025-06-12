using Eduva.Domain.Common;
using Eduva.Domain.Enums;

namespace Eduva.Domain.Entities
{
    public class Folder : BaseTimestampedEntity<int>
    {
        public Guid? UserId { get; set; } = Guid.Empty; // Used only for personal folders
        public Guid? ClassId { get; set; } = Guid.Empty; // Used only for class folders
        public OwnerType OwnerType { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order {  get; set; }

        public virtual User? User { get; set; }
        public virtual Classroom? Class { get; set; }
        public virtual ICollection<FolderLessonMaterial> FolderLessonMaterials { get; set; } = [];
    }
}
