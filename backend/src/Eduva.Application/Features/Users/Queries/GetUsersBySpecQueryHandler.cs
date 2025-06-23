using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Users.Responses;
using Eduva.Application.Features.Users.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using MediatR;

namespace Eduva.Application.Features.Users.Queries
{
    public class GetUsersBySpecQueryHandler
        : IRequestHandler<GetUsersBySpecQuery, Pagination<UserResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetUsersBySpecQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Pagination<UserResponse>> Handle(GetUsersBySpecQuery request, CancellationToken cancellationToken)
        {
            var spec = new UserSpecification(request.Param);

            var result = await _unitOfWork
                .GetRepository<ApplicationUser, Guid>()
                .GetWithSpecAsync(spec);

            return AppMapper.Mapper.Map<Pagination<UserResponse>>(result);
        }
    }
}