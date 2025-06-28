using Eduva.Application.Common.Specifications;
using Eduva.Domain.Enums;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Payments.Specifications
{
    public class MyPaymentSpecParam : BaseSpecParam
    {
        [JsonIgnore]
        public Guid UserId { get; set; }

        public PaymentPurpose? PaymentPurpose { get; set; }

        public PaymentStatus? PaymentStatus { get; set; }

        public DateFilter DateFilter { get; set; } = DateFilter.All;
    }
}