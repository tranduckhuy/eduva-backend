using Eduva.Application.Common.Specifications;
using Eduva.Domain.Entities;
using System.Linq.Expressions;

namespace Eduva.Application.Features.CreditTransactions.Specifications
{
    public class CreditTransactionSpecification : ISpecification<UserCreditTransaction>
    {
        public Expression<Func<UserCreditTransaction, bool>> Criteria { get; private set; }
        public Func<IQueryable<UserCreditTransaction>, IOrderedQueryable<UserCreditTransaction>>? OrderBy { get; private set; }
        public List<Expression<Func<UserCreditTransaction, object>>> Includes { get; private set; } = [];
        public Func<IQueryable<UserCreditTransaction>, IQueryable<UserCreditTransaction>>? Selector => null;
        public int Skip { get; private set; }
        public int Take { get; private set; }

        public CreditTransactionSpecification(CreditTransactionSpecParam param)
        {
            Criteria = BuildCriteria(param);
            OrderBy = BuildOrderBy(param);
            Skip = (param.PageIndex - 1) * param.PageSize;
            Take = param.PageSize;

            Includes.Add(x => x.User);
            Includes.Add(x => x.AICreditPack);
            Includes.Add(x => x.PaymentTransaction);
        }

        private static Expression<Func<UserCreditTransaction, bool>> BuildCriteria(CreditTransactionSpecParam param)
        {
            return tx =>
                (param.UserId == Guid.Empty || tx.UserId == param.UserId) &&
                (param.AICreditPackId == 0 || tx.AICreditPackId == param.AICreditPackId);
        }

        private static Func<IQueryable<UserCreditTransaction>, IOrderedQueryable<UserCreditTransaction>>? BuildOrderBy(CreditTransactionSpecParam param)
        {
            if (string.IsNullOrWhiteSpace(param.SortBy))
                return q => q.OrderByDescending(tx => tx.CreatedAt); // Default sort

            var sort = param.SortBy.ToLower();
            bool isDesc = param.SortDirection.ToLower() == "desc";

            return sort switch
            {
                "createdat" => isDesc
                    ? q => q.OrderByDescending(tx => tx.CreatedAt)
                    : q => q.OrderBy(tx => tx.CreatedAt),

                "credits" => isDesc
                    ? q => q.OrderByDescending(tx => tx.Credits)
                    : q => q.OrderBy(tx => tx.Credits),

                _ => isDesc
                    ? q => q.OrderByDescending(tx => tx.CreatedAt)
                    : q => q.OrderBy(tx => tx.CreatedAt)
            };
        }
    }
}