using Eduva.Application.Common.Specifications;
using Eduva.Domain.Enums;

namespace Eduva.API.Models.LessonMaterials
{
    public class GetOwnLessonMaterialsRequest : BaseSpecParam
    {
        public EntityStatus EntityStatus { get; set; } = EntityStatus.Active;
    }
}
