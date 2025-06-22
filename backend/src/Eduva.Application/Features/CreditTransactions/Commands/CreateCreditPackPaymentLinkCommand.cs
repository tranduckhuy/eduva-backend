using Eduva.Application.Features.CreditTransactions.Responses;
using Eduva.Shared.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.CreditTransactions.Commands
{
    public class CreateCreditPackPaymentLinkCommand : IRequest<(CustomCode, CreateCreditPackPaymentLinkResponse)>
    {
        public int CreditPackId { get; set; }

        [JsonIgnore]
        public Guid UserId { get; set; }
    }
}