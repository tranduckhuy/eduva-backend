using Eduva.Application.Common.Specifications;
using Eduva.Domain.Enums;

namespace Eduva.Application.Features.LessonMaterials
{
    public class GetLessonMaterialApprovalsRequest : BaseSpecParam
    {
        public Guid? LessonMaterialId { get; set; }
        public Guid? ApproverId { get; set; }
        public LessonMaterialStatus? StatusChangeTo { get; set; }
    }
}
