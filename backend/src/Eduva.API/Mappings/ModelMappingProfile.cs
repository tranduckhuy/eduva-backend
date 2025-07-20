using AutoMapper;
using Eduva.API.Models.Jobs;
using Eduva.API.Models.LessonMaterials;
using Eduva.Application.Features.Jobs.Specifications;
using Eduva.Application.Features.LessonMaterials.Specifications;

namespace Eduva.API.Mappings
{
    public class ModelMappingProfile : Profile
    {
        public ModelMappingProfile()
        {
            CreateMap<GetSchoolPublicLessonMaterialsRequest, LessonMaterialSpecParam>();
            CreateMap<GetPendingLessonMaterialsRequest, LessonMaterialSpecParam>();
            CreateMap<GetOwnLessonMaterialsRequest, LessonMaterialSpecParam>();
            CreateMap<GetCompletedJobsRequest, JobSpecParam>();
        }
    }
}
