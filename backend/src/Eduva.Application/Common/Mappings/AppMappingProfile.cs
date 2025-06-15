using AutoMapper;
using Eduva.Application.Features.LessonMaterials.Commands;
using Eduva.Application.Features.LessonMaterials.Responses;
using Eduva.Domain.Entities;

namespace Eduva.Application.Common.Mappings
{
    public class AppMappingProfile : Profile
    {
        public AppMappingProfile()
        {
            CreateMap<CreateLessonMaterialCommand, LessonMaterial>();
            CreateMap<LessonMaterial, LessonMaterialResponse>().ReverseMap();
        }
    }
}
