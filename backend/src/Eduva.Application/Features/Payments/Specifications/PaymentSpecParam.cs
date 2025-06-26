using Eduva.Application.Common.Specifications;
using Eduva.Domain.Enums;

namespace Eduva.Application.Features.Payments.Specifications
{
    public class PaymentSpecParam : BaseSpecParam
    {
        public PaymentPurpose? PaymentPurpose { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }
    }
}