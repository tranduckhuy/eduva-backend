using AutoMapper;
using Eduva.API.Models.LessonMaterials;
using Eduva.Application.Features.LessonMaterials.Specifications;

namespace Eduva.API.Mappings
{
    public class ModelMappingProfile : Profile
    {
        public ModelMappingProfile()
        {
            CreateMap<GetSchoolPublicLessonMaterialsRequest, LessonMaterialSpecParam>();
            CreateMap<GetPendingLessonMaterialsRequest, LessonMaterialSpecParam>();
        }
    }
}
