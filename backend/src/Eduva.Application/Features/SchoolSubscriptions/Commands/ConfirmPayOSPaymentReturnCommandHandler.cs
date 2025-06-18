using Eduva.Application.Exceptions.School;
using Eduva.Application.Exceptions.SchoolSubscription;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;

namespace Eduva.Application.Features.SchoolSubscriptions.Commands
{
    public class ConfirmPayOSPaymentReturnCommandHandler : IRequestHandler<ConfirmPayOSPaymentReturnCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ConfirmPayOSPaymentReturnCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(ConfirmPayOSPaymentReturnCommand request, CancellationToken cancellationToken)
        {
            var subRepo = _unitOfWork.GetCustomRepository<ISchoolSubscriptionRepository>();
            var schoolRepo = _unitOfWork.GetRepository<School, int>();

            if (request.Code != "00" || request.Status != "PAID")
            {
                throw new PaymentFailedException();
            }

            var subscription = await subRepo.FindByTransactionIdAsync(request.OrderCode.ToString())
                ?? throw new SchoolSubscriptionNotFoundException();

            if (subscription.PaymentStatus == PaymentStatus.Paid)
            {
                throw new PaymentAlreadyConfirmedException();
            }

            var oldActiveSub = await subRepo.GetActiveSubscriptionBySchoolIdAsync(subscription.SchoolId);
            if (oldActiveSub != null && oldActiveSub.Id != subscription.Id)
            {
                oldActiveSub.SubscriptionStatus = SubscriptionStatus.Expired;
                oldActiveSub.EndDate = DateTimeOffset.UtcNow;
            }

            subscription.PaymentStatus = PaymentStatus.Paid;
            subscription.SubscriptionStatus = SubscriptionStatus.Active;
            subscription.LastUsageResetDate = DateTimeOffset.UtcNow;

            var school = await schoolRepo.GetByIdAsync(subscription.SchoolId) ?? throw new SchoolNotFoundException();
            if (school != null && school.Status == EntityStatus.Inactive)
            {
                school.Status = EntityStatus.Active;
            }

            await _unitOfWork.CommitAsync();
            return Unit.Value;
        }
    }
}