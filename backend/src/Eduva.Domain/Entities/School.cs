using Eduva.Domain.Common;
using Eduva.Domain.Enums;

namespace Eduva.Domain.Entities
{
    public class School : BaseTimestampedEntity<int>
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string? WebsiteUrl { get; set; }

        // Navigation properties
        public virtual ICollection<SchoolSubscription> SchoolSubscriptions { get; set; } = [];
        public virtual ICollection<User> Users { get; set; } = [];
        public virtual ICollection<Classroom> Classes { get; set; } = [];
        public virtual ICollection<LessonMaterial> LessonMaterials { get; set; } = [];
    }
}
