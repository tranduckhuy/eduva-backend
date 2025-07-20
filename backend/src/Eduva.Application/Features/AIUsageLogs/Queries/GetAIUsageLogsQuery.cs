using Eduva.Application.Common.Models;
using Eduva.Application.Features.AIUsageLogs.Specifications;
using MediatR;

namespace Eduva.Application.Features.AIUsageLogs.Queries
{
    public class GetAIUsageLogsQuery : IRequest<Pagination<AIUsageLogResponse>>
    {
        public AIUsageLogSpecParam SpecParam { get; set; } = default!;
        public Guid UserId { get; set; } = Guid.Empty;
    }
}
