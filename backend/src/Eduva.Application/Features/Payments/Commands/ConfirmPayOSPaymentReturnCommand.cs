using MediatR;

namespace Eduva.Application.Features.Payments.Commands
{
    public class ConfirmPayOSPaymentReturnCommand : IRequest<Unit>
    {
        public string Code { get; set; } = default!;
        public string Id { get; set; } = default!;
        public string Status { get; set; } = default!;
        public long OrderCode { get; set; }
    }
}