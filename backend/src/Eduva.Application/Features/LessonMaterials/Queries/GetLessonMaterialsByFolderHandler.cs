using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.LessonMaterials.Queries.Extensions;
using Eduva.Application.Features.LessonMaterials.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using MediatR;

namespace Eduva.Application.Features.LessonMaterials.Queries
{
    public class GetLessonMaterialsByFolderHandler : IRequestHandler<GetLessonMaterialsByFolderQuery, IReadOnlyList<LessonMaterialResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetLessonMaterialsByFolderHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<LessonMaterialResponse>> Handle(GetLessonMaterialsByFolderQuery request, CancellationToken cancellationToken)
        {
            var lessonMaterialRepository = _unitOfWork.GetCustomRepository<ILessonMaterialRepository>();

            // System Admin, School Admin, Content Moderator can access all materials in a folder of a school
            if (request.UserRoles.HasSystemAdminRole() || request.UserRoles.HasSchoolAdminRole() || request.UserRoles.HasContentModeratorRole())
            {
                var allMaterials = await lessonMaterialRepository.GetLessonMaterialsByFolderAsync(
                    request.FolderId,
                    request.SchoolId,
                    request.FilterOptions,
                    cancellationToken);

                return AppMapper<AppMappingProfile>.Mapper.Map<IReadOnlyList<LessonMaterialResponse>>(allMaterials);
            }

            // Teacher can only access their own materials in a folder
            if (request.UserRoles.HasTeacherRole())
            {
                var teacherMaterials = await lessonMaterialRepository.GetLessonMaterialsByFolderForTeacherAsync(
                    request.FolderId,
                    request.UserId,
                    request.SchoolId,
                    request.FilterOptions,
                    cancellationToken);

                return AppMapper<AppMappingProfile>.Mapper.Map<IReadOnlyList<LessonMaterialResponse>>(teacherMaterials);
            }

            // Student can only access materials shared with them in a folder
            if (request.UserRoles.HasStudentRole())
            {
                var studentMaterials = await lessonMaterialRepository.GetLessonMaterialsByFolderForStudentAsync(
                    request.FolderId,
                    request.UserId,
                    request.SchoolId,
                    request.FilterOptions,
                    cancellationToken);

                return AppMapper<AppMappingProfile>.Mapper.Map<IReadOnlyList<LessonMaterialResponse>>(studentMaterials);
            }

            return new List<LessonMaterialResponse>();
        }
    }
}
