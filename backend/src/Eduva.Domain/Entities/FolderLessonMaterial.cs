using Eduva.Domain.Common;

namespace Eduva.Domain.Entities
{
    public class FolderLessonMaterial : BaseEntity<int>
    {
        public int FolderID { get; set; }
        public int LessonMaterialID { get; set; }
        public int Order {  get; set; }

        // Navigation properties
        public virtual Folder Folder { get; set; } = default!;
        public virtual LessonMaterial LessonMaterial { get; set; } = default!;
    }
}
