using Net.payOS.Types;

namespace Eduva.Application.Features.SchoolSubscriptions.Configurations.PayOS
{
    public interface IPayOSService
    {
        Task<CreatePaymentResult> CreatePaymentLinkAsync(PaymentData data);
    }

}