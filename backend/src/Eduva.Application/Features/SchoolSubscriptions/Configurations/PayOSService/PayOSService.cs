using Net.payOS;
using Net.payOS.Types;

namespace Eduva.Application.Features.SchoolSubscriptions.Configurations.PayOSService
{
    public class PayOSService : IPayOSService
    {
        private readonly PayOS _payOS;

        public PayOSService(PayOS payOS)
        {
            _payOS = payOS;
        }

        public Task<CreatePaymentResult> CreatePaymentLinkAsync(PaymentData data)
        {
            return _payOS.createPaymentLink(data);
        }
    }
}