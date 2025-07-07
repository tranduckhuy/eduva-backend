using AutoMapper;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.AICreditPacks.Responses;
using Eduva.Application.Features.Classes.Commands.CreateClass;
using Eduva.Application.Features.Classes.Commands.UpdateClass;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Features.CreditTransactions.Responses;
using Eduva.Application.Features.Folders.Responses;
using Eduva.Application.Features.LessonMaterials;
using Eduva.Application.Features.LessonMaterials.Commands;
using Eduva.Application.Features.LessonMaterials.Responses;
using Eduva.Application.Features.Payments.Responses;
using Eduva.Application.Features.Questions.Commands.CreateQuestionComment;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Features.Schools.Commands.CreateSchool;
using Eduva.Application.Features.Schools.Responses;
using Eduva.Application.Features.SchoolSubscriptions.Responses;
using Eduva.Application.Features.SubscriptionPlans.Responses;
using Eduva.Application.Features.SystemConfigs;
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
                .ForMember(dest => dest.School, opt => opt.MapFrom(src => src.School))
                .ForMember(dest => dest.CreditBalance, opt => opt.MapFrom(src => src.TotalCredits))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.LastModifiedAt, opt => opt.MapFrom(src => src.LastModifiedAt))
                .ForMember(dest => dest.LastLoginAt, opt => opt.MapFrom(src => src.LastLoginAt));
            CreateMap<Pagination<ApplicationUser>, Pagination<UserResponse>>();

            // Lesson Materials mappings
            CreateMap<CreateLessonMaterialCommand, LessonMaterial>();
            CreateMap<LessonMaterialRequest, LessonMaterial>();
            CreateMap<LessonMaterial, LessonMaterialResponse>()
                .ForMember(dest => dest.CreatedById, opt => opt.MapFrom(src => src.CreatedByUserId))
                .ForMember(dest => dest.CreatedByName,
                        opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.FullName : string.Empty))
                .ReverseMap();
            CreateMap<Pagination<LessonMaterial>, Pagination<LessonMaterialResponse>>();

            // Subscription Plan mappings
            CreateMap<Pagination<SubscriptionPlan>, Pagination<SubscriptionPlanResponse>>();
            CreateMap<SubscriptionPlan, SubscriptionPlanResponse>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.LastModifiedAt, opt => opt.MapFrom(src => src.LastModifiedAt));

            // AICreditPack mappings
            CreateMap<Pagination<AICreditPack>, Pagination<AICreditPackResponse>>();
            CreateMap<AICreditPack, AICreditPackResponse>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.LastModifiedAt, opt => opt.MapFrom(src => src.LastModifiedAt));

            // School mappings
            CreateMap<CreateSchoolCommand, SchoolResponse>();
            CreateMap<Pagination<School>, Pagination<SchoolResponse>>();
            CreateMap<School, SchoolResponse>();

            // SchoolSubscription mappings
            CreateMap<SchoolSubscription, MySchoolSubscriptionResponse>()
                .ForMember(dest => dest.PlanName, opt => opt.MapFrom(src => src.Plan.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Plan.Description))
                .ForMember(dest => dest.MaxUsers, opt => opt.MapFrom(src => src.Plan.MaxUsers))
                .ForMember(dest => dest.StorageLimitGB, opt => opt.MapFrom(src => src.Plan.StorageLimitGB))
                .ForMember(dest => dest.AmountPaid, opt => opt.MapFrom(src => src.PaymentTransaction.Amount))
                .ForMember(dest => dest.PlanId, opt => opt.MapFrom(src => src.Plan.Id))
                .ForMember(dest => dest.PriceMonthly, otp => otp.MapFrom(src => src.Plan.PriceMonthly))
                .ForMember(dest => dest.PricePerYear, otp => otp.MapFrom(src => src.Plan.PricePerYear));

            // SchoolSubscription → SchoolSubscriptionResponse
            CreateMap<SchoolSubscription, SchoolSubscriptionResponse>()
            .ForMember(dest => dest.School, opt => opt.MapFrom(src => new SchoolInfo
            {
                Id = src.School.Id,
                Name = src.School.Name,
                Address = src.School.Address,
                ContactEmail = src.School.ContactEmail,
                ContactPhone = src.School.ContactPhone,
                WebsiteUrl = src.School.WebsiteUrl
            }))
            .ForMember(dest => dest.Plan, opt => opt.MapFrom(src => new SubscriptionPlanInfo
            {
                Id = src.Plan.Id,
                Name = src.Plan.Name,
                Description = src.Plan.Description,
                MaxUsers = src.Plan.MaxUsers,
                StorageLimitGB = src.Plan.StorageLimitGB,
                Price = src.BillingCycle == BillingCycle.Monthly ? src.Plan.PriceMonthly : src.Plan.PricePerYear,
                IsRecommended = src.Plan.IsRecommended
            }))
            .ForMember(dest => dest.PaymentTransaction, opt => opt.MapFrom(src => new PaymentTransactionInfo
            {
                UserId = src.PaymentTransaction.UserId,
                PaymentPurpose = src.PaymentTransaction.PaymentPurpose,
                PaymentItemId = src.PaymentTransaction.PaymentItemId,
                RelatedId = src.PaymentTransaction.RelatedId,
                PaymentMethod = src.PaymentTransaction.PaymentMethod,
                PaymentStatus = src.PaymentTransaction.PaymentStatus,
                TransactionCode = src.PaymentTransaction.TransactionCode,
                Amount = src.PaymentTransaction.Amount,
                CreatedAt = src.PaymentTransaction.CreatedAt
            }))
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => new UserInfo
            {
                Id = src.PaymentTransaction.User.Id,
                FullName = src.PaymentTransaction.User.FullName ?? string.Empty,
                Email = src.PaymentTransaction.User.Email ?? string.Empty,
                PhoneNumber = src.PaymentTransaction.User.PhoneNumber ?? string.Empty,
                AvatarUrl = src.PaymentTransaction.User.AvatarUrl ?? string.Empty
            }));
            CreateMap<Pagination<SchoolSubscription>, Pagination<SchoolSubscriptionResponse>>();

            // UserCreditTransaction mappings
            CreateMap<UserCreditTransaction, CreditTransactionResponse>()
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => new UserInfo
            {
                Id = src.User.Id,
                FullName = src.User.FullName ?? string.Empty,
                Email = src.User.Email ?? string.Empty,
                PhoneNumber = src.User.PhoneNumber ?? string.Empty,
                AvatarUrl = src.User.AvatarUrl ?? string.Empty
            }))
            .ForMember(dest => dest.AICreditPack, opt => opt.MapFrom(src => new AICreditPackInfor
            {
                Id = src.AICreditPack.Id,
                Name = src.AICreditPack.Name,
                Price = src.AICreditPack.Price,
                Credits = src.AICreditPack.Credits,
                BonusCredits = src.AICreditPack.BonusCredits
            }));

            CreateMap<Pagination<UserCreditTransaction>, Pagination<CreditTransactionResponse>>();

            // Payment mappings
            CreateMap<PaymentTransaction, PaymentResponse>()
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => new UserInfo
            {
                Id = src.User.Id,
                FullName = src.User.FullName ?? string.Empty,
                Email = src.User.Email ?? string.Empty,
                PhoneNumber = src.User.PhoneNumber ?? string.Empty,
                AvatarUrl = src.User.AvatarUrl ?? string.Empty
            }));
            CreateMap<Pagination<PaymentTransaction>, Pagination<PaymentResponse>>();

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
                .ForMember(dest => dest.BackgroundImageUrl, opt => opt.MapFrom(src => src.BackgroundImageUrl))
                .ForMember(dest => dest.SchoolName, opt => opt.MapFrom(src => src.School != null ? src.School.Name : string.Empty))
                .ForMember(dest => dest.TeacherAvatarUrl, opt => opt.MapFrom(src => src.Teacher != null ? src.Teacher.AvatarUrl : null))
                .ReverseMap();
            CreateMap<Pagination<Classroom>, Pagination<ClassResponse>>();

            // Question mappings
            CreateMap<LessonMaterialQuestion, QuestionResponse>()
                .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.FullName : string.Empty))
                .ForMember(dest => dest.CreatedByAvatar, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.AvatarUrl : null))
                .ForMember(dest => dest.LessonMaterialTitle, opt => opt.MapFrom(src => src.LessonMaterial != null ? src.LessonMaterial.Title : string.Empty))
                .ForMember(dest => dest.CommentCount, opt => opt.MapFrom(src => src.Comments.Count));
            CreateMap<Pagination<LessonMaterialQuestion>, Pagination<QuestionResponse>>();

            // Question Detail mappings
            CreateMap<LessonMaterialQuestion, QuestionDetailResponse>()
                .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.FullName : string.Empty))
                .ForMember(dest => dest.CreatedByAvatar, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.AvatarUrl : null))
                .ForMember(dest => dest.LessonMaterialTitle, opt => opt.MapFrom(src => src.LessonMaterial != null ? src.LessonMaterial.Title : string.Empty))
                .ForMember(dest => dest.CreatedByRole, opt => opt.Ignore())
                .ForMember(dest => dest.CommentCount, opt => opt.Ignore())
                .ForMember(dest => dest.CanUpdate, opt => opt.Ignore())
                .ForMember(dest => dest.CanDelete, opt => opt.Ignore())
                .ForMember(dest => dest.CanComment, opt => opt.Ignore())
                .ForMember(dest => dest.Comments, opt => opt.Ignore());

            CreateMap<CreateQuestionCommentCommand, QuestionComment>();

            // Question Comment mappings
            CreateMap<QuestionComment, QuestionCommentResponse>()
               .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
               .ForMember(dest => dest.QuestionId, opt => opt.MapFrom(src => src.QuestionId))
               .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
               .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
               .ForMember(dest => dest.LastModifiedAt, opt => opt.MapFrom(src => src.LastModifiedAt))
               .ForMember(dest => dest.CreatedByUserId, opt => opt.MapFrom(src => src.CreatedByUserId))
               .ForMember(dest => dest.ParentCommentId, opt => opt.MapFrom(src => src.ParentCommentId))
               .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.FullName : string.Empty))
               .ForMember(dest => dest.CreatedByAvatar, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.AvatarUrl : null))
               .ForMember(dest => dest.CreatedByRole, opt => opt.Ignore())
               .ForMember(dest => dest.CanUpdate, opt => opt.Ignore())
               .ForMember(dest => dest.CanDelete, opt => opt.Ignore())
               .ForMember(dest => dest.Replies, opt => opt.Ignore())
               .ForMember(dest => dest.ReplyCount, opt => opt.Ignore());

            // Question Reply mappings  
            CreateMap<QuestionComment, QuestionReplyResponse>()
                 .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.LastModifiedAt, opt => opt.MapFrom(src => src.LastModifiedAt))
                .ForMember(dest => dest.CreatedByUserId, opt => opt.MapFrom(src => src.CreatedByUserId))
                .ForMember(dest => dest.ParentCommentId, opt => opt.MapFrom(src => src.ParentCommentId!.Value))
                .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.FullName : string.Empty))
                .ForMember(dest => dest.CreatedByAvatar, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.AvatarUrl : null))
                .ForMember(dest => dest.CreatedByRole, opt => opt.Ignore())
                .ForMember(dest => dest.CanUpdate, opt => opt.Ignore())
                .ForMember(dest => dest.CanDelete, opt => opt.Ignore());

            // StudentClass mappings
            CreateMap<StudentClass, StudentClassResponse>()
                .ForMember(dest => dest.ClassName, opt => opt.MapFrom(src => src.Class.Name))
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src => src.Class.Teacher != null ? src.Class.Teacher.FullName ?? string.Empty : string.Empty))
                .ForMember(dest => dest.SchoolName, opt => opt.MapFrom(src => src.Class.School != null ? src.Class.School.Name : string.Empty))
                .ForMember(dest => dest.ClassCode, opt => opt.MapFrom(src => src.Class.ClassCode ?? string.Empty))
                .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student != null ? src.Student.FullName : string.Empty))
                .ForMember(dest => dest.TeacherAvatarUrl, opt => opt.MapFrom(src => src.Class.Teacher != null ? src.Class.Teacher.AvatarUrl : null))
                .ForMember(dest => dest.StudentAvatarUrl, opt => opt.MapFrom(src => src.Student != null ? src.Student.AvatarUrl : null))
                .ForMember(dest => dest.ClassStatus, opt => opt.MapFrom(src => src.Class.Status))
                .ForMember(dest => dest.BackgroundImageUrl, opt => opt.MapFrom(src => src.Class != null ? src.Class.BackgroundImageUrl : string.Empty))
                .ForMember(dest => dest.CountLessonMaterial, opt => opt.Ignore());
            CreateMap<Pagination<StudentClass>, Pagination<StudentClassResponse>>();

            // Folder mappings
            CreateMap<Folder, FolderResponse>()
                .ForMember(dest => dest.OwnerName, opt => opt.MapFrom(src => GetOwnerName(src)))
                .ForMember(dest => dest.CountLessonMaterial, opt => opt.Ignore());
            CreateMap<Pagination<Folder>, Pagination<FolderResponse>>();

            // System Configurations
            CreateMap<CreateSystemConfigDto, SystemConfig>();
            CreateMap<SystemConfig, SystemConfigDto>();
            CreateMap<UpdateSystemConfigDto, SystemConfig>();
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