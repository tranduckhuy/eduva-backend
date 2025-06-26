using Eduva.Application.Common.Specifications;
using Eduva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Eduva.Application.Features.Payments.Specifications
{
    public class PaymentSpecification : ISpecification<PaymentTransaction>
    {
        public Expression<Func<PaymentTransaction, bool>> Criteria { get; private set; }
        public Func<IQueryable<PaymentTransaction>, IOrderedQueryable<PaymentTransaction>>? OrderBy { get; private set; }
        public List<Expression<Func<PaymentTransaction, object>>> Includes { get; private set; } = [];
        public Func<IQueryable<PaymentTransaction>, IQueryable<PaymentTransaction>>? Selector => null;
        public int Skip { get; private set; }
        public int Take { get; private set; }

        public PaymentSpecification(PaymentSpecParam param)
        {
            Criteria = BuildCriteria(param);
            OrderBy = BuildOrderBy(param);
            Skip = (param.PageIndex - 1) * param.PageSize;
            Take = param.PageSize;

            Includes.Add(p => p.User); // Include navigation property
        }

        private static Expression<Func<PaymentTransaction, bool>> BuildCriteria(PaymentSpecParam param)
        {
            var loweredSearch = param.SearchTerm?.ToLower() ?? string.Empty;

            return p =>
                (!param.PaymentPurpose.HasValue || p.PaymentPurpose == param.PaymentPurpose) &&
                (!param.PaymentMethod.HasValue || p.PaymentMethod == param.PaymentMethod) &&
                (!param.PaymentStatus.HasValue || p.PaymentStatus == param.PaymentStatus) &&
                (string.IsNullOrWhiteSpace(loweredSearch) ||
                    EF.Functions.Like((p.User.FullName ?? "").ToLower(), $"%{loweredSearch}%") ||
                    EF.Functions.Like((p.User.Email ?? "").ToLower(), $"%{loweredSearch}%"));
        }

        private static Func<IQueryable<PaymentTransaction>, IOrderedQueryable<PaymentTransaction>>? BuildOrderBy(PaymentSpecParam param)
        {
            if (string.IsNullOrWhiteSpace(param.SortBy)) return null;

            bool isDesc = param.SortDirection.ToLower() == "desc";
            string sort = param.SortBy.ToLower();

            return sort switch
            {
                "fullname" => isDesc
                    ? q => q.OrderByDescending(p => p.User.FullName)
                    : q => q.OrderBy(p => p.User.FullName),

                "email" => isDesc
                    ? q => q.OrderByDescending(p => p.User.Email)
                    : q => q.OrderBy(p => p.User.Email),

                "amount" => isDesc
                    ? q => q.OrderByDescending(p => p.Amount)
                    : q => q.OrderBy(p => p.Amount),

                "createdat" => isDesc
                    ? q => q.OrderByDescending(p => p.CreatedAt)
                    : q => q.OrderBy(p => p.CreatedAt),

                _ => q => q.OrderByDescending(p => p.CreatedAt)
            };
        }
    }
}