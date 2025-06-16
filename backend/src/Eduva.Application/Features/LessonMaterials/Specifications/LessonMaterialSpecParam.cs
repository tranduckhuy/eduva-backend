using Eduva.Application.Common.Specifications;
using Eduva.Domain.Enums;

namespace Eduva.Application.Features.LessonMaterials.Specifications
{
    public class LessonMaterialSpecParam : BaseSpecParam
    {
        public int? SchoolId { get; set; }
        public Guid? CreatedBy { get; set; }
        public string? SortBy { get; set; }
        public string? SearchTerm { get; set; }
        public string? Tag { get; set; }
        public ContentType? ContentType { get; set; }
        public LessonMaterialStatus? LessonStatus { get; set; }
        public LessonMaterialVisibility? Visibility { get; set; }
    }
}
