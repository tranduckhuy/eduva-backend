using Eduva.Application.Common.Mappings;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Features.Users.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using MediatR;

namespace Eduva.Application.Features.Users.Commands
{
    public class UpdateUserProfileHandler : IRequestHandler<UpdateUserProfileCommand, UserResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateUserProfileHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }        public async Task<UserResponse> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.GetCustomRepository<IUserRepository>().GetByIdAsync(request.UserId)
                ?? throw new UserNotExistsException();

            // Only update fields that are not null or empty (partial update support)
            if (!string.IsNullOrEmpty(request.FullName))
            {
                user.FullName = request.FullName;
            }

            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                user.PhoneNumber = request.PhoneNumber;
            }

            if (!string.IsNullOrEmpty(request.AvatarUrl))
            {
                user.AvatarUrl = request.AvatarUrl;
            }

            _unitOfWork.GetCustomRepository<IUserRepository>().Update(user);
            await _unitOfWork.CommitAsync();

            return AppMapper.Mapper.Map<UserResponse>(user);
        }
    }
}
