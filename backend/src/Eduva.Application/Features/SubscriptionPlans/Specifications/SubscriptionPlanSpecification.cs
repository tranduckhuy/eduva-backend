using Eduva.Application.Common.Specifications;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Microsoft.EntityFrameworkCore;
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
            var loweredSearchTerm = param.SearchTerm?.ToLower();

            return sp =>
                (param.ActiveOnly == null ||
                 (param.ActiveOnly.Value && sp.Status == EntityStatus.Active) ||
                 (!param.ActiveOnly.Value && sp.Status != EntityStatus.Active)) &&
                (string.IsNullOrWhiteSpace(loweredSearchTerm) ||
                 EF.Functions.Like(sp.Name.ToLower(), $"%{loweredSearchTerm}%"));
        }

        private static Func<IQueryable<SubscriptionPlan>, IOrderedQueryable<SubscriptionPlan>>? BuildOrderBy(SubscriptionPlanSpecParam param)
        {
            if (string.IsNullOrWhiteSpace(param.SortBy))
                return null;

            bool isDescending = param.SortDirection?.ToLower() == "desc";
            string sortBy = param.SortBy.ToLower();

            return sortBy switch
            {
                "name" => isDescending
                    ? q => q.OrderByDescending(x => x.Name)
                    : q => q.OrderBy(x => x.Name),

                "storage" => isDescending
                    ? q => q.OrderByDescending(x => x.StorageLimitGB)
                    : q => q.OrderBy(x => x.StorageLimitGB),

                "users" => isDescending
                    ? q => q.OrderByDescending(x => x.MaxUsers)
                    : q => q.OrderBy(x => x.MaxUsers),

                "monthly" => isDescending
                    ? q => q.OrderByDescending(x => x.PriceMonthly)
                    : q => q.OrderBy(x => x.PriceMonthly),

                "yearly" => isDescending
                    ? q => q.OrderByDescending(x => x.PricePerYear)
                    : q => q.OrderBy(x => x.PricePerYear),

                _ => isDescending
                    ? q => q.OrderByDescending(x => x.CreatedAt)
                    : q => q.OrderBy(x => x.CreatedAt)
            };
        }
    }
}