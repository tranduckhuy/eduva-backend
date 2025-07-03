using Net.payOS.Types;

namespace Eduva.Application.Interfaces.Services
{
    public interface IPayOSService
    {
        Task<CreatePaymentResult> CreatePaymentLinkAsync(PaymentData data);
    }
}