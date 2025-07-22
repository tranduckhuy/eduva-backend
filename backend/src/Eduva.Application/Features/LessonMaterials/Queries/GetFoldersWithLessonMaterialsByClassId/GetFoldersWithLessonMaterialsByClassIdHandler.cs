using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Features.LessonMaterials.Queries.Extensions;
using Eduva.Application.Features.LessonMaterials.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;

namespace Eduva.Application.Features.LessonMaterials.Queries.GetFoldersWithLessonMaterialsByClassId
{
    public class GetFoldersWithLessonMaterialsByClassIdHandler
        : IRequestHandler<GetFoldersWithLessonMaterialsByClassIdQuery, IReadOnlyList<FolderWithLessonMaterialsResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public GetFoldersWithLessonMaterialsByClassIdHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<FolderWithLessonMaterialsResponse>> Handle(GetFoldersWithLessonMaterialsByClassIdQuery request, CancellationToken cancellationToken)
        {
            var classRepository = _unitOfWork.GetCustomRepository<IClassroomRepository>();

            var classEntity = await classRepository.GetByIdAsync(request.ClassId);

            if (classEntity?.SchoolId != request.SchoolId)
            {
                throw new ForbiddenException(["You do not have permission to access this class."]);
            }

            if (request.UserRoles.HasStudentRole())
            {
                var studentClassRepository = _unitOfWork.GetCustomRepository<IStudentClassRepository>();
                var isEnrolled = await studentClassRepository
                .IsStudentEnrolledInClassAsync(request.UserId, request.ClassId);

                if (!isEnrolled)
                    return [];
            }

            var folderRepository = _unitOfWork.GetCustomRepository<IFolderRepository>();
            var folders = await folderRepository
                .GetFoldersWithLessonMaterialsByClassIdAsync(request.ClassId);

            var allMaterials = folders
                .SelectMany(f => f.FolderLessonMaterials
                    .OrderBy(flm => flm.LessonMaterial.CreatedAt)
                    .Select(flm => flm.LessonMaterial))
                    .Where(lm => IsLessonMaterialVisibleToUser(lm, request))
                .DistinctBy(m => m.Id)
                .ToList();

            var allResponses = allMaterials.MapWithNextPrev();

            var result = folders.Select(folder =>
            {
                var folderMaterialIds = folder.FolderLessonMaterials
                    .Select(flm => flm.LessonMaterialId)
                    .ToHashSet();

                var folderMaterials = allResponses
                    .Where(r => folderMaterialIds.Contains(r.Id))
                    .ToList();

                return new FolderWithLessonMaterialsResponse
                {
                    Id = folder.Id,
                    Name = folder.Name,
                    CountLessonMaterials = folderMaterials.Count,
                    LessonMaterials = folderMaterials
                };
            }).ToList();

            return result;
        }

        private static bool IsLessonMaterialVisibleToUser(LessonMaterial lm, GetFoldersWithLessonMaterialsByClassIdQuery request)
        {
            if (request.UserRoles.HasStudentRole())
            {
                return lm.Status == EntityStatus.Active &&
                       lm.LessonStatus == LessonMaterialStatus.Approved;
            }

            var matchStatus = !request.Status.HasValue || lm.Status == request.Status;
            var matchLessonStatus = !request.LessonStatus.HasValue || lm.LessonStatus == request.LessonStatus;

            return matchStatus && matchLessonStatus;
        }
    }
}
