using Eduva.Application.Common.Specifications;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using System.Linq.Expressions;

namespace Eduva.Application.Features.SchoolSubscriptions.Specifications
{
    public class SchoolSubscriptionSpecification : ISpecification<SchoolSubscription>
    {
        public Expression<Func<SchoolSubscription, bool>> Criteria { get; private set; }
        public Func<IQueryable<SchoolSubscription>, IOrderedQueryable<SchoolSubscription>>? OrderBy { get; private set; }
        public List<Expression<Func<SchoolSubscription, object>>> Includes { get; private set; } = [];
        public Func<IQueryable<SchoolSubscription>, IQueryable<SchoolSubscription>>? Selector => null;
        public int Skip { get; private set; }
        public int Take { get; private set; }

        public SchoolSubscriptionSpecification(SchoolSubscriptionSpecParam param)
        {
            Criteria = BuildCriteria(param);
            OrderBy = BuildOrderBy(param);
            Skip = (param.PageIndex - 1) * param.PageSize;
            Take = param.PageSize;

            Includes.Add(x => x.School);
            Includes.Add(x => x.Plan);
            Includes.Add(x => x.PaymentTransaction);
        }

        private static Expression<Func<SchoolSubscription, bool>> BuildCriteria(SchoolSubscriptionSpecParam param)
        {
            var baseUtc = DateTimeOffset.UtcNow;

            DateTimeOffset? from = param.DateFilter switch
            {
                DateFilter.Today => baseUtc,
                DateFilter.Last7Days => baseUtc.AddDays(-6),
                DateFilter.Last30Days => baseUtc.AddDays(-29),
                _ => null
            };

            var to = baseUtc.AddDays(1); // exclusive upper bound for today, 7d, 30d

            return s =>
                (!param.SubscriptionStatus.HasValue || s.SubscriptionStatus == param.SubscriptionStatus) &&
                (!param.BillingCycle.HasValue || s.BillingCycle == param.BillingCycle) &&
                (
                    param.DateFilter == DateFilter.All ||
                    (from.HasValue && s.CreatedAt >= from.Value && s.CreatedAt < to)
                );
        }

        private static Func<IQueryable<SchoolSubscription>, IOrderedQueryable<SchoolSubscription>>? BuildOrderBy(SchoolSubscriptionSpecParam param)
        {
            if (string.IsNullOrWhiteSpace(param.SortBy))
                return null;

            var sort = param.SortBy.ToLower();
            bool isDesc = param.SortDirection.ToLower() == "desc";

            return sort switch
            {
                "startdate" => isDesc
                    ? q => q.OrderByDescending(s => s.StartDate)
                    : q => q.OrderBy(s => s.StartDate),

                "enddate" => isDesc
                    ? q => q.OrderByDescending(s => s.EndDate)
                    : q => q.OrderBy(s => s.EndDate),

                "createdat" => isDesc
                    ? q => q.OrderByDescending(s => s.CreatedAt)
                    : q => q.OrderBy(s => s.CreatedAt),

                "amount" => isDesc
                    ? q => q.OrderByDescending(s => s.PaymentTransaction.Amount)
                    : q => q.OrderBy(s => s.PaymentTransaction.Amount),

                _ => isDesc
                    ? q => q.OrderByDescending(s => s.CreatedAt)
                    : q => q.OrderBy(s => s.CreatedAt)
            };
        }
    }
}