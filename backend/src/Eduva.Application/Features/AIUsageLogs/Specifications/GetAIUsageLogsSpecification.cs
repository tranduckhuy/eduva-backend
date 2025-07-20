using Eduva.Application.Common.Specifications;
using Eduva.Domain.Entities;
using System.Linq.Expressions;

namespace Eduva.Application.Features.AIUsageLogs.Specifications
{
    public class GetAIUsageLogsSpecification : ISpecification<AIUsageLog>
    {
        public Expression<Func<AIUsageLog, bool>> Criteria { get; private set; }
        public Func<IQueryable<AIUsageLog>, IOrderedQueryable<AIUsageLog>>? OrderBy { get; private set; }
        public List<Expression<Func<AIUsageLog, object>>> Includes { get; private set; } = [];
        public Func<IQueryable<AIUsageLog>, IQueryable<AIUsageLog>>? Selector { get; private set; }
        public int Skip { get; private set; }
        public int Take { get; private set; }

        public GetAIUsageLogsSpecification(AIUsageLogSpecParam param, Guid userId)
        {
            Criteria = job => job.UserId == userId;

            OrderBy = q => q.OrderByDescending(j => j.CreatedAt);

            if (param.IsPagingEnabled)
            {
                Skip = (param.PageIndex - 1) * param.PageSize;
                Take = param.PageSize;
            }
            else
            {
                Skip = 0;
                Take = int.MaxValue;
            }
        }
    }
}
