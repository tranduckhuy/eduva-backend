using Eduva.Application.Features.SchoolSubscriptions.Responses;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;

namespace Eduva.Application.Features.SchoolSubscriptions.Commands
{
    public class CreateSchoolSubscriptionCommand : IRequest<(CustomCode, CreatePaymentLinkResponse)>
    {
        public int PlanId { get; set; }
        public int SchoolId { get; set; }
        public BillingCycle BillingCycle { get; set; }
    }
}