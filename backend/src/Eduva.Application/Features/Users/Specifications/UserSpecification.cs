using Eduva.Application.Common.Specifications;
using Eduva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Eduva.Application.Features.Users.Specifications
{
    public class UserSpecification : ISpecification<ApplicationUser>
    {
        public Expression<Func<ApplicationUser, bool>> Criteria { get; private set; }
        public Func<IQueryable<ApplicationUser>, IOrderedQueryable<ApplicationUser>>? OrderBy { get; private set; }
        public List<Expression<Func<ApplicationUser, object>>> Includes { get; private set; } = [];
        public Func<IQueryable<ApplicationUser>, IQueryable<ApplicationUser>>? Selector { get; init; }
        public int Skip { get; private set; }
        public int Take { get; private set; }

        public UserSpecification(UserSpecParam param)
        {
            Criteria = BuildCriteria(param);
            OrderBy = BuildOrderBy(param);
            Skip = (param.PageIndex - 1) * param.PageSize;
            Take = param.PageSize;

            Includes.Add(u => u.School!);
        }

        private static Expression<Func<ApplicationUser, bool>> BuildCriteria(UserSpecParam param)
        {
            var loweredSearch = param.SearchTerm?.ToLower() ?? string.Empty;

            return u =>
                (param.SchoolId == null || u.SchoolId == param.SchoolId) &&
                (string.IsNullOrWhiteSpace(loweredSearch) ||
                    EF.Functions.Like((u.FullName ?? "").ToLower(), $"%{loweredSearch}%") ||
                    EF.Functions.Like((u.Email ?? "").ToLower(), $"%{loweredSearch}%"));
        }

        private static Func<IQueryable<ApplicationUser>, IOrderedQueryable<ApplicationUser>>? BuildOrderBy(UserSpecParam param)
        {
            if (string.IsNullOrWhiteSpace(param.SortBy))
                return null;

            bool isDesc = param.SortDirection.ToLower() == "desc";
            string sort = param.SortBy.ToLower();

            return sort switch
            {
                "fullname" => isDesc
                    ? q => q.OrderByDescending(u => u.FullName)
                    : q => q.OrderBy(u => u.FullName),

                "email" => isDesc
                    ? q => q.OrderByDescending(u => u.Email)
                    : q => q.OrderBy(u => u.Email),

                _ => isDesc
                    ? q => q.OrderByDescending(u => u.FullName)
                    : q => q.OrderBy(u => u.FullName)
            };
        }
    }
}