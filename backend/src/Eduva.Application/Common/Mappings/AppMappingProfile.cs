using AutoMapper;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.AICreditPacks.Responses;
using Eduva.Application.Features.Classes.Commands.CreateClass;
using Eduva.Application.Features.Classes.Commands.UpdateClass;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Features.Folders.Responses;
using Eduva.Application.Features.LessonMaterials;
using Eduva.Application.Features.LessonMaterials.Commands;
using Eduva.Application.Features.LessonMaterials.Responses;
using Eduva.Application.Features.Payments.Responses;
using Eduva.Application.Features.Schools.Commands.CreateSchool;
using Eduva.Application.Features.Schools.Responses;
using Eduva.Application.Features.StudentClasses.Responses;
using Eduva.Application.Features.SubscriptionPlans.Responses;
using Eduva.Application.Features.Users.Responses;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;

namespace Eduva.Application.Common.Mappings
{
    public class AppMappingProfile : Profile
    {
        public AppMappingProfile()
        {
            // User mappings
            CreateMap<ApplicationUser, UserResponse>()
                .ForMember(dest => dest.School, opt => opt.MapFrom(src => src.School));
            CreateMap<Pagination<ApplicationUser>, Pagination<UserResponse>>();

            // Lesson Materials mappings
            CreateMap<CreateLessonMaterialCommand, LessonMaterial>();
            CreateMap<LessonMaterialRequest, LessonMaterial>();
            CreateMap<LessonMaterial, LessonMaterialResponse>()
                .ForMember(dest => dest.CreatedById, opt => opt.MapFrom(src => src.CreatedBy))
                .ForMember(dest => dest.CreatedByName,
                        opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.FullName : string.Empty))
                .ReverseMap();
            CreateMap<Pagination<LessonMaterial>, Pagination<LessonMaterialResponse>>();

            // Subscription Plan mappings
            CreateMap<Pagination<SubscriptionPlan>, Pagination<SubscriptionPlanResponse>>();
            CreateMap<SubscriptionPlan, SubscriptionPlanResponse>();

            // AICreditPack mappings
            CreateMap<Pagination<AICreditPack>, Pagination<AICreditPackResponse>>();
            CreateMap<AICreditPack, AICreditPackResponse>();

            // School mappings
            CreateMap<CreateSchoolCommand, School>();
            CreateMap<Pagination<School>, Pagination<SchoolResponse>>();
            CreateMap<School, SchoolResponse>();

            // SchoolSubscription mappings
            CreateMap<SchoolSubscription, MySchoolSubscriptionResponse>()
                .ForMember(dest => dest.PlanName, opt => opt.MapFrom(src => src.Plan.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Plan.Description))
                .ForMember(dest => dest.MaxUsers, opt => opt.MapFrom(src => src.Plan.MaxUsers))
                .ForMember(dest => dest.StorageLimitGB, opt => opt.MapFrom(src => src.Plan.StorageLimitGB))
                .ForMember(dest => dest.AmountPaid, opt => opt.MapFrom(src => src.PaymentTransaction.Amount));

            // Class mappings
            CreateMap<CreateClassCommand, Classroom>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ClassCode, opt => opt.Ignore());
            CreateMap<UpdateClassCommand, Classroom>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.SchoolId, opt => opt.Ignore())
                .ForMember(dest => dest.ClassCode, opt => opt.Ignore())
                .ForMember(dest => dest.TeacherId, opt => opt.Ignore());
            CreateMap<Classroom, ClassResponse>()
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src => src.Teacher != null ? src.Teacher.FullName : string.Empty))
                .ForMember(dest => dest.SchoolName, opt => opt.MapFrom(src => src.School != null ? src.School.Name : string.Empty))
                .ReverseMap();
            CreateMap<Pagination<Classroom>, Pagination<ClassResponse>>();

            // StudentClass mappings
            CreateMap<StudentClass, StudentClassResponse>()
                .ForMember(dest => dest.ClassName, opt => opt.MapFrom(src => src.Class.Name))
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src => src.Class.Teacher != null ? src.Class.Teacher.FullName ?? string.Empty : string.Empty))
                .ForMember(dest => dest.SchoolName, opt => opt.MapFrom(src => src.Class.School != null ? src.Class.School.Name : string.Empty))
                .ForMember(dest => dest.ClassCode, opt => opt.MapFrom(src => src.Class.ClassCode ?? string.Empty))
                .ForMember(dest => dest.ClassStatus, opt => opt.MapFrom(src => src.Class.Status));
            CreateMap<Pagination<StudentClass>, Pagination<StudentClassResponse>>();

            // Folder mappings
            CreateMap<Folder, FolderResponse>()
                .ForMember(dest => dest.OwnerName, opt => opt.MapFrom(src => GetOwnerName(src)));
            CreateMap<Pagination<Folder>, Pagination<FolderResponse>>();
        }
        private static string GetOwnerName(Folder folder)
        {
            if (folder.OwnerType == OwnerType.Personal)
            {
                return folder.User != null ? GetUserFullName(folder.User) : string.Empty;
            }

            return folder.Class != null ? GetClassName(folder.Class) : string.Empty;
        }

        private static string GetUserFullName(ApplicationUser user)
        {
            return user != null ? user.FullName ?? string.Empty : string.Empty;
        }

        private static string GetClassName(Classroom classroom)
        {
            return classroom != null ? classroom.Name ?? string.Empty : string.Empty;
        }
    }
}