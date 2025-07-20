using Eduva.Application.Common.Specifications;
using Eduva.Domain.Enums;

namespace Eduva.API.Models.LessonMaterials
{
    public class GetSchoolPublicLessonMaterialsRequest : BaseSpecParam
    {
        public Guid? CreatedByUserId { get; set; }
        public ContentType? ContentType { get; set; }
        public EntityStatus EntityStatus { get; set; } = EntityStatus.Active;
    }
}
