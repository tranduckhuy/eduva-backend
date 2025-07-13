using AutoMapper;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Folders.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Folders.Queries
{
    public class GetAllFoldersByClassIdHandler : IRequestHandler<GetAllFoldersByClassIdQuery, IEnumerable<FolderResponse>>
    {
        private readonly IFolderRepository _folderRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public GetAllFoldersByClassIdHandler(
            IFolderRepository folderRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager)
        {
            _folderRepository = folderRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IEnumerable<FolderResponse>> Handle(GetAllFoldersByClassIdQuery request, CancellationToken cancellationToken)
        {
            await CheckClassFolderAccessAsync(request.UserId, request.ClassId);

            var allFolders = await _folderRepository.GetAllAsync();
            var folders = allFolders.Where(f =>
                f.OwnerType == OwnerType.Class &&
                f.ClassId.HasValue &&
                f.ClassId.Value == request.ClassId).ToList();

            var folderResponses = _mapper.Map<IEnumerable<FolderResponse>>(folders);

            var folderResponseList = folderResponses.ToList();
            if (folderResponseList.Count > 0)
            {
                var folderIds = folderResponseList.Select(f => f.Id).ToList();

                var lessonMaterialRepo = _unitOfWork.GetCustomRepository<ILessonMaterialRepository>();
                var countsByFolder = await lessonMaterialRepo.GetApprovedMaterialCountsByFolderAsync(folderIds, cancellationToken);

                foreach (var folderResponse in folderResponseList)
                {
                    folderResponse.CountLessonMaterial = countsByFolder.TryGetValue(folderResponse.Id, out var count)
                        ? count
                        : 0;
                }
            }

            return folderResponseList;
        }

        private async Task CheckClassFolderAccessAsync(Guid userId, Guid classId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new AppException(CustomCode.UserNotExists);
            }

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains(nameof(Role.SystemAdmin)))
            {
                return;
            }

            var classRepository = _unitOfWork.GetRepository<Classroom, Guid>();
            var classroom = await classRepository.GetByIdAsync(classId);

            if (classroom == null)
            {
                throw new AppException(CustomCode.ClassNotFound);
            }

            if (roles.Contains(nameof(Role.SchoolAdmin)) && classroom.SchoolId == user.SchoolId)
            {
                return;
            }

            if ((roles.Contains(nameof(Role.Teacher)) || roles.Contains(nameof(Role.ContentModerator))) && classroom.TeacherId == userId)
            {
                return;
            }

            if (roles.Contains(nameof(Role.Student)))
            {
                var studentClassRepository = _unitOfWork.GetCustomRepository<IStudentClassRepository>();
                bool isEnrolled = await studentClassRepository.IsStudentEnrolledInClassAsync(userId, classId);

                if (isEnrolled)
                {
                    return;
                }
            }

            throw new AppException(CustomCode.Forbidden);
        }
    }
}