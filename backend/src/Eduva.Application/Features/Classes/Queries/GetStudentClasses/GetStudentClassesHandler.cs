using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Features.Classes.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;

namespace Eduva.Application.Features.Classes.Queries.GetStudentClasses
{
    public class GetStudentClassesHandler : IRequestHandler<GetStudentClassesQuery, Pagination<StudentClassResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetStudentClassesHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Pagination<StudentClassResponse>> Handle(GetStudentClassesQuery request, CancellationToken cancellationToken)
        {
            // Ensure StudentId is set in the spec params
            request.StudentClassSpecParam.StudentId = request.StudentId;

            // Create specification
            var spec = new StudentClassSpecification(request.StudentClassSpecParam);

            // Get repository and use GetWithSpecAsync method
            var result = await _unitOfWork.GetCustomRepository<IStudentClassRepository>()
                .GetWithSpecAsync(spec);

            // Map to response model
            var studentClasses = AppMapper.Mapper.Map<Pagination<StudentClassResponse>>(result);

            if (studentClasses.Data.Count > 0)
            {
                var classIds = studentClasses.Data.Select(sc => sc.ClassId).ToList();

                var folderRepository = _unitOfWork.GetRepository<Folder, Guid>();
                var allFolders = await folderRepository.GetAllAsync();
                var classFolders = allFolders.Where(f =>
                    f.OwnerType == OwnerType.Class &&
                    f.ClassId.HasValue &&
                    classIds.Contains(f.ClassId.Value)
                ).ToList();

                if (classFolders.Count > 0)
                {
                    var foldersByClass = classFolders
                        .GroupBy(f => f.ClassId!.Value)
                        .ToDictionary(g => g.Key, g => g.Select(f => f.Id).ToList());

                    var allFolderIds = classFolders.Select(f => f.Id).ToList();

                    if (allFolderIds.Count > 0)
                    {
                        var folderLessonMaterialRepo = _unitOfWork.GetRepository<FolderLessonMaterial, int>();
                        var allFolderLessonMaterials = await folderLessonMaterialRepo.GetAllAsync();
                        var relevantFolderLessonMaterials = allFolderLessonMaterials
                            .Where(flm => allFolderIds.Contains(flm.FolderId))
                            .ToList();

                        var countsByFolder = relevantFolderLessonMaterials
                            .GroupBy(flm => flm.FolderId)
                            .ToDictionary(g => g.Key, g => g.Count());

                        foreach (var studentClassResponse in studentClasses.Data)
                        {
                            int totalCount = 0;

                            if (foldersByClass.TryGetValue(studentClassResponse.ClassId, out var classFolderIds))
                            {
                                foreach (var folderId in classFolderIds)
                                {
                                    if (countsByFolder.TryGetValue(folderId, out var count))
                                    {
                                        totalCount += count;
                                    }
                                }
                            }

                            studentClassResponse.CountLessonMaterial = totalCount;
                        }
                    }
                }
            }
            return studentClasses;
        }
    }
}
