using Eduva.Application.Common.Specifications;
using Eduva.Domain.Enums;

namespace Eduva.Application.Features.LessonMaterials.Specifications
{
    public class LessonMaterialApprovalsSpecParam : BaseSpecParam
    {
        public Guid? LessonMaterialId { get; set; }
        public Guid? ApproverId { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public LessonMaterialStatus? StatusChangeTo { get; set; }
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? ToDate { get; set; }
        public int? SchoolId { get; set; }
    }
}