using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Features.Classes.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;

namespace Eduva.Application.Features.Classes.Queries.GetTeacherClasses
{
    public class GetTeacherClassesHandler : IRequestHandler<GetTeacherClassesQuery, Pagination<ClassResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetTeacherClassesHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Pagination<ClassResponse>> Handle(GetTeacherClassesQuery request, CancellationToken cancellationToken)
        {
            // Create ClassSpecParam with TeacherId
            var classSpecParam = request.ClassSpecParam;
            classSpecParam.TeacherId = request.TeacherId;

            var spec = new ClassSpecification(classSpecParam);

            var result = await _unitOfWork.GetCustomRepository<IClassroomRepository>()
                .GetWithSpecAsync(spec);

            var classrooms = AppMapper.Mapper.Map<Pagination<ClassResponse>>(result);

            if (classrooms.Data.Count > 0)
            {
                var classIds = classrooms.Data.Select(c => c.Id).ToList();

                var folderRepository = _unitOfWork.GetRepository<Folder, Guid>();
                var allFolders = await folderRepository.GetAllAsync();
                var folders = allFolders.Where(f =>
                    f.OwnerType == OwnerType.Class &&
                    f.ClassId.HasValue &&
                    classIds.Contains(f.ClassId.Value)
                ).ToList();

                if (folders.Count > 0)
                {
                    var foldersByClass = folders.GroupBy(f => f.ClassId!.Value)
                        .ToDictionary(g => g.Key, g => g.Select(f => f.Id).ToList());

                    var allFolderIds = folders.Select(f => f.Id).ToList();

                    if (allFolderIds.Count > 0)
                    {
                        var lessonMaterialRepo = _unitOfWork.GetCustomRepository<ILessonMaterialRepository>();
                        var countsByFolder = await lessonMaterialRepo.GetApprovedMaterialCountsByFolderAsync(allFolderIds, cancellationToken);

                        foreach (var classResponse in classrooms.Data)
                        {
                            int totalCount = 0;

                            if (foldersByClass.TryGetValue(classResponse.Id, out var classFolderIds))
                            {
                                foreach (var folderId in classFolderIds)
                                {
                                    if (countsByFolder.TryGetValue(folderId, out var count))
                                    {
                                        totalCount += count;
                                    }
                                }
                            }

                            classResponse.CountLessonMaterial = totalCount;
                        }
                    }
                }
            }

            return classrooms;
        }
    }
}
