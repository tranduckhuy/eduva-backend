using Eduva.Domain.Common;

namespace Eduva.Domain.Entities
{
    public class FolderLessonMaterial : BaseEntity<Guid>
    {
        public int FolderID { get; set; }
        public Guid LessonMaterialID { get; set; }

        // Navigation properties
        public virtual Folder Folder { get; set; } = default!;
        public virtual LessonMaterial LessonMaterial { get; set; } = default!;
    }
}