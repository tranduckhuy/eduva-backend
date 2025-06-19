using Eduva.Application.Features.SchoolSubscriptions.Configurations.PayOS;
using Net.payOS;
using Net.payOS.Types;

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