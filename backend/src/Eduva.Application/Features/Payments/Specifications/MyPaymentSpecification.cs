using Eduva.Application.Common.Specifications;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using System.Linq.Expressions;

namespace Eduva.Application.Features.Payments.Specifications
{
    public class MyPaymentSpecification : ISpecification<PaymentTransaction>
    {
        public Expression<Func<PaymentTransaction, bool>> Criteria { get; private set; }
        public Func<IQueryable<PaymentTransaction>, IOrderedQueryable<PaymentTransaction>>? OrderBy { get; private set; }
        public List<Expression<Func<PaymentTransaction, object>>> Includes { get; private set; } = [];
        public Func<IQueryable<PaymentTransaction>, IQueryable<PaymentTransaction>>? Selector => null;
        public int Skip { get; private set; }
        public int Take { get; private set; }

        public MyPaymentSpecification(MyPaymentSpecParam param)
        {
            Criteria = BuildCriteria(param);
            OrderBy = BuildOrderBy(param);
            Skip = (param.PageIndex - 1) * param.PageSize;
            Take = param.PageSize;

            Includes.Add(x => x.User);
        }

        private static Expression<Func<PaymentTransaction, bool>> BuildCriteria(MyPaymentSpecParam param)
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

            return x =>
                x.UserId == param.UserId &&
                (!param.PaymentPurpose.HasValue || x.PaymentPurpose == param.PaymentPurpose) &&
                (!param.PaymentStatus.HasValue || x.PaymentStatus == param.PaymentStatus) &&
                (
                    param.DateFilter == DateFilter.All ||
                    (from.HasValue && x.CreatedAt >= from.Value && x.CreatedAt < to)
                );
        }

        private static Func<IQueryable<PaymentTransaction>, IOrderedQueryable<PaymentTransaction>>? BuildOrderBy(MyPaymentSpecParam param)
        {
            if (string.IsNullOrWhiteSpace(param.SortBy))
                return null;

            var sort = param.SortBy.ToLower();
            var isDesc = param.SortDirection.ToLower() == "desc";

            return sort switch
            {
                "createdat" => isDesc
                    ? q => q.OrderByDescending(x => x.CreatedAt)
                    : q => q.OrderBy(x => x.CreatedAt),

                "amount" => isDesc
                    ? q => q.OrderByDescending(x => x.Amount)
                    : q => q.OrderBy(x => x.Amount),

                _ => isDesc
                    ? q => q.OrderByDescending(x => x.CreatedAt)
                    : q => q.OrderBy(x => x.CreatedAt)
            };
        }
    }
}