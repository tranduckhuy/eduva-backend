using Eduva.Application.Features.Payments.Responses;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Payments.Commands
{
    public class CreateSchoolSubscriptionCommand : IRequest<(CustomCode, CreatePaymentLinkResponse)>
    {
        public int PlanId { get; set; }
        public int SchoolId { get; set; }
        public BillingCycle BillingCycle { get; set; }

        [JsonIgnore]
        public Guid UserId { get; set; }
    }
}