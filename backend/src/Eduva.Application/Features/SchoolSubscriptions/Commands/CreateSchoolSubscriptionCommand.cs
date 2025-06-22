using Eduva.Application.Features.SchoolSubscriptions.Responses;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.SchoolSubscriptions.Commands
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