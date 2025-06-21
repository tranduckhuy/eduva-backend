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
        }

        public async Task<UserResponse> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.GetCustomRepository<IUserRepository>().GetByIdAsync(request.UserId)
                ?? throw new UserNotExistsException();

            user.FullName = request.FullName ?? user.FullName;
            user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
            user.AvatarUrl = request.AvatarUrl ?? user.AvatarUrl;
            _unitOfWork.GetCustomRepository<IUserRepository>().Update(user);

            await _unitOfWork.CommitAsync();

            return AppMapper.Mapper.Map<UserResponse>(user);
        }
    }
}
