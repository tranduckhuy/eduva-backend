using AutoMapper;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.LessonMaterials.Commands;
using Eduva.Application.Features.LessonMaterials.Responses;
using Eduva.Application.Features.Schools.Commands;
using Eduva.Application.Features.Schools.Reponses;
using Eduva.Application.Features.SubscriptionPlans.Responses;
using Eduva.Domain.Entities;

namespace Eduva.Application.Common.Mappings
{
    public class AppMappingProfile : Profile
    {
        public AppMappingProfile()
        {
            CreateMap<CreateLessonMaterialCommand, LessonMaterial>();
            CreateMap<LessonMaterial, LessonMaterialResponse>()
                .ForMember(dest => dest.CreatedById, opt => opt.MapFrom(src => src.CreatedBy))
                .ForMember(dest => dest.CreatedByName,
                        opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.FullName : string.Empty))
                .ReverseMap();
            CreateMap<Pagination<LessonMaterial>, Pagination<LessonMaterialResponse>>();
            CreateMap<CreateSchoolCommand, School>();
            CreateMap<School, SchoolResponse>();
            CreateMap<Pagination<SubscriptionPlan>, Pagination<SubscriptionPlanResponse>>();
            CreateMap<SubscriptionPlan, SubscriptionPlanResponse>();
        }
    }
}