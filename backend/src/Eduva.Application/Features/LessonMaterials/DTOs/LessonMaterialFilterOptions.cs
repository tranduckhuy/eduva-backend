using Eduva.Domain.Enums;

namespace Eduva.Application.Features.LessonMaterials.DTOs
{
    public class LessonMaterialFilterOptions
    {
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; }
        public LessonMaterialStatus? LessonStatus { get; set; }
        public EntityStatus? Status { get; set; }
    }
}
