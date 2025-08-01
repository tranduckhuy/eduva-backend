using Eduva.Application.Common.Specifications;
using Eduva.Domain.Enums;

namespace Eduva.Application.Features.Classes.Specifications
{
    public class ClassSpecParam : BaseSpecParam
    {
        public int? SchoolId { get; set; }
        public Guid? TeacherId { get; set; }
        public Guid? ClassId { get; set; }
        public EntityStatus? Status { get; set; }
    }
}
