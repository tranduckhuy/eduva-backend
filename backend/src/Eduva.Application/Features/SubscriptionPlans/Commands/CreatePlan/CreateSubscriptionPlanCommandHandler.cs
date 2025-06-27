using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.SubscriptionPlans.Responses;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;

namespace Eduva.Application.Features.SubscriptionPlans.Commands.CreatePlan
{
    public class CreateSubscriptionPlanCommandHandler : IRequestHandler<CreateSubscriptionPlanCommand, SubscriptionPlanResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateSubscriptionPlanCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SubscriptionPlanResponse> Handle(CreateSubscriptionPlanCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<SubscriptionPlan, int>();

            var plan = new SubscriptionPlan
            {
                Name = request.Name,
                Description = request.Description,
                MaxUsers = request.MaxUsers,
                StorageLimitGB = request.StorageLimitGB,
                PriceMonthly = request.PriceMonthly,
                PricePerYear = request.PricePerYear,
                IsRecommended = request.IsRecommended,
                Status = EntityStatus.Active
            };

            await repo.AddAsync(plan);
            await _unitOfWork.CommitAsync();

            return AppMapper.Mapper.Map<SubscriptionPlanResponse>(plan);
        }
    }
}