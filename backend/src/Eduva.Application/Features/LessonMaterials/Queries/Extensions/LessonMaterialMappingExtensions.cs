using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.LessonMaterials.Responses;
using Eduva.Domain.Entities;

namespace Eduva.Application.Features.LessonMaterials.Queries.Extensions
{
    public static class LessonMaterialMappingExtensions
    {
        public static List<LessonMaterialResponse> MapWithNextPrev(this IReadOnlyList<LessonMaterial> materials)
        {
            var mapped = AppMapper<AppMappingProfile>.Mapper
                .Map<List<LessonMaterialResponse>>(materials);

            return mapped.Select((item, index) =>
            {
                item.PreviousLessonMaterialId = index > 0 ? mapped[index - 1].Id : null;
                item.NextLessonMaterialId = index < mapped.Count - 1 ? mapped[index + 1].Id : null;
                return item;
            }).ToList();
        }
    }
}
