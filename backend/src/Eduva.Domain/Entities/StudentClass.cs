using Eduva.Domain.Common;
using System.Security.Claims;

namespace Eduva.Domain.Entities
{
    public class StudentClass : BaseEntity<int>
    {
        public Guid StudentId { get; set; }
        public Guid ClassId { get; set; }
        public DateTimeOffset EnrolledAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public virtual User Student { get; set; } = null!;
        public virtual Classroom Class { get; set; } = null!;
    }
}
