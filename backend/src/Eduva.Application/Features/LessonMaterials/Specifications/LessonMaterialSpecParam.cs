using Eduva.Application.Common.Specifications;
using Eduva.Domain.Enums;

namespace Eduva.Application.Features.LessonMaterials.Specifications
{
    public class LessonMaterialSpecParam : BaseSpecParam
    {
        public Guid? CreatedByUserId { get; set; }
        public ContentType? ContentType { get; set; }
        public LessonMaterialStatus? LessonStatus { get; set; }
        public LessonMaterialVisibility? Visibility { get; set; }
        public Guid? ClassId { get; set; }
        public Guid? FolderId { get; set; }
        public int SchoolId { get; set; }
        public EntityStatus EntityStatus { get; set; } = EntityStatus.Active;
    }
}
