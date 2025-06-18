using Eduva.Application.Common.Specifications;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using System.Linq.Expressions;

namespace Eduva.Application.Features.SubscriptionPlans.Specifications
{
    public class SubscriptionPlanSpecification : ISpecification<SubscriptionPlan>
    {
        public Expression<Func<SubscriptionPlan, bool>> Criteria { get; private set; }
        public Func<IQueryable<SubscriptionPlan>, IOrderedQueryable<SubscriptionPlan>>? OrderBy { get; private set; }
        public List<Expression<Func<SubscriptionPlan, object>>> Includes { get; private set; } = [];
        public Func<IQueryable<SubscriptionPlan>, IQueryable<SubscriptionPlan>>? Selector { get; init; }
        public int Skip { get; private set; }
        public int Take { get; private set; }

        public SubscriptionPlanSpecification(SubscriptionPlanSpecParam param)
        {
            Criteria = BuildCriteria(param);
            OrderBy = BuildOrderBy(param);
            Skip = (param.PageIndex - 1) * param.PageSize;
            Take = param.PageSize;
        }

        private static Expression<Func<SubscriptionPlan, bool>> BuildCriteria(SubscriptionPlanSpecParam param)
        {
            return sp =>
                (param.ActiveOnly == null ||
                 (param.ActiveOnly.Value && sp.Status == EntityStatus.Active) ||
                 (!param.ActiveOnly.Value && sp.Status != EntityStatus.Active)) &&
                (string.IsNullOrWhiteSpace(param.SearchTerm) ||
                 sp.Name.Contains(param.SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        private static Func<IQueryable<SubscriptionPlan>, IOrderedQueryable<SubscriptionPlan>>? BuildOrderBy(SubscriptionPlanSpecParam param)
        {
            if (string.IsNullOrWhiteSpace(param.SortBy))
                return null;

            bool isDescending = param.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

            return param.SortBy.ToLower() switch
            {
                "name" => isDescending
                    ? q => q.OrderByDescending(x => x.Name)
                    : q => q.OrderBy(x => x.Name),
                "price" => isDescending
                    ? q => q.OrderByDescending(x => x.PriceMonthly)
                    : q => q.OrderBy(x => x.PriceMonthly),
                _ => isDescending
                    ? q => q.OrderByDescending(x => x.CreatedAt)
                    : q => q.OrderBy(x => x.CreatedAt)
            };
        }
    }
}