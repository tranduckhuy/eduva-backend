using Eduva.Application.Common.Models;
using Eduva.Application.Features.Payments.Responses;
using Eduva.Application.Features.Payments.Specifications;
using MediatR;

namespace Eduva.Application.Features.Payments.Queries
{
    public record GetMyPaymentsQuery(MyPaymentSpecParam SpecParam) : IRequest<Pagination<PaymentResponse>>;
}