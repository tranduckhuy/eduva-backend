using AutoMapper;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Exceptions.School;
using Eduva.Application.Exceptions.SchoolSubscription;
using Eduva.Application.Features.Payments.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using MediatR;

namespace Eduva.Application.Features.Payments.Queries
{
    public class GetMySchoolSubscriptionQueryHandler : IRequestHandler<GetMySchoolSubscriptionQuery, MySchoolSubscriptionResponse>
    {
        private readonly ISchoolSubscriptionRepository _schoolSubscriptionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public GetMySchoolSubscriptionQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _userRepository = unitOfWork.GetCustomRepository<IUserRepository>();
            _schoolSubscriptionRepository = unitOfWork.GetCustomRepository<ISchoolSubscriptionRepository>();
            _mapper = mapper;
        }

        public async Task<MySchoolSubscriptionResponse> Handle(GetMySchoolSubscriptionQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId)
                  ?? throw new UserNotExistsException();

            if (user.SchoolId is null)
            {
                throw new SchoolNotFoundException();
            }

            var sub = await _schoolSubscriptionRepository.GetLatestPaidBySchoolIdAsync(user.SchoolId.Value, cancellationToken)
                       ?? throw new SchoolSubscriptionNotFoundException();

            return _mapper.Map<MySchoolSubscriptionResponse>(sub);
        }
    }
}