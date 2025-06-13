using Eduva.Domain.Common;

namespace Eduva.Domain.Entities
{
    public class Classroom : BaseTimestampedEntity<Guid>
    {
        public int SchoolId { get; set; }
        public string Name { get; set; } = default!;
        public string? ClassCode { get; set; } 
        public Guid TeacherId { get; set; }

        // Navigation properties
        public virtual School School { get; set; } = default!;
        public virtual ApplicationUser Teacher { get; set; } = default!;
        public virtual ICollection<StudentClass> StudentClasses { get; set; } = [];
        public virtual ICollection<Folder> ClassFolders { get; set; } = [];
    }
}
