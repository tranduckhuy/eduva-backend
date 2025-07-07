using Eduva.Application.Common.Specifications;
using Eduva.Domain.Enums;

namespace Eduva.API.Models.LessonMaterials
{
    public class GetPendingLessonMaterialsRequest : BaseSpecParam
    {
        public string? Tag { get; set; }
        public ContentType? ContentType { get; set; }
        public Guid? ClassId { get; set; }
        public Guid? FolderId { get; set; }
    }
}
