using Net.payOS.Types;

namespace Eduva.Application.Features.SchoolSubscriptions.Configurations.PayOSService
{
    public interface IPayOSService
    {
        Task<CreatePaymentResult> CreatePaymentLinkAsync(PaymentData data);
    }
}