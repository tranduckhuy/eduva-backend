using AutoMapper;
using Eduva.Application.Features.Folders.Responses;
using Eduva.Application.Features.Folders.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using MediatR;

namespace Eduva.Application.Features.Folders.Queries
{
    public class GetAllUserFoldersHandler : IRequestHandler<GetAllUserFoldersQuery, List<FolderResponse>>
    {
        private readonly IFolderRepository _folderRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public GetAllUserFoldersHandler(
            IFolderRepository folderRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork)
        {
            _folderRepository = folderRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<FolderResponse>> Handle(GetAllUserFoldersQuery request, CancellationToken cancellationToken)
        {
            var folderSpecParam = request.FolderSpecParam;
            folderSpecParam.IsPagingEnabled = false;

            var spec = new FolderSpecification(folderSpecParam);

            var folderPagination = await _folderRepository.GetWithSpecAsync(spec);

            var folders = folderPagination?.Data ?? new List<Domain.Entities.Folder>();


            var data = _mapper.Map<List<FolderResponse>>(folders);

            if (data != null && data.Count > 0)
            {
                var folderIds = data.Select(f => f.Id).ToList();

                if (folderIds.Count > 0)
                {
                    var lessonMaterialRepo = _unitOfWork.GetCustomRepository<ILessonMaterialRepository>();
                    if (lessonMaterialRepo != null)
                    {
                        var countsByFolder = await lessonMaterialRepo
                            .GetApprovedMaterialCountsByFolderAsync(folderIds, cancellationToken);

                        foreach (var folderResponse in data)
                        {
                            folderResponse.CountLessonMaterial =
                                countsByFolder.TryGetValue(folderResponse.Id, out var count) ? count : 0;
                        }
                    }
                }
            }

            return data ?? new List<FolderResponse>();
        }
    }
}
