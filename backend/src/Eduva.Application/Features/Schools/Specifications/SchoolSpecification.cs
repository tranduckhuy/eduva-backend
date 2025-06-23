using Eduva.Application.Common.Specifications;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Eduva.Application.Features.Schools.Specifications
{
    public class SchoolSpecification : ISpecification<School>
    {
        public Expression<Func<School, bool>> Criteria { get; private set; }
        public Func<IQueryable<School>, IOrderedQueryable<School>>? OrderBy { get; private set; }
        public List<Expression<Func<School, object>>> Includes { get; private set; } = [];
        public Func<IQueryable<School>, IQueryable<School>>? Selector { get; init; }
        public int Skip { get; private set; }
        public int Take { get; private set; }

        public SchoolSpecification(SchoolSpecParam param)
        {
            Criteria = BuildCriteria(param);
            OrderBy = BuildOrderBy(param);
            Skip = (param.PageIndex - 1) * param.PageSize;
            Take = param.PageSize;
        }

        private static Expression<Func<School, bool>> BuildCriteria(SchoolSpecParam param)
        {
            var loweredTerm = param.SearchTerm?.ToLower();

            return s =>
                (!param.ActiveOnly.HasValue ||
                 (param.ActiveOnly.Value && s.Status == EntityStatus.Active) ||
                 (!param.ActiveOnly.Value && s.Status != EntityStatus.Active)) &&
                (string.IsNullOrWhiteSpace(loweredTerm) ||
                 EF.Functions.Like(s.Name.ToLower(), $"%{loweredTerm}%") ||
                 EF.Functions.Like(s.ContactEmail.ToLower(), $"%{loweredTerm}%") ||
                 EF.Functions.Like(s.ContactPhone.ToLower(), $"%{loweredTerm}%"));
        }

        private static Func<IQueryable<School>, IOrderedQueryable<School>>? BuildOrderBy(SchoolSpecParam param)
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

                "contactemail" => isDescending
                    ? q => q.OrderByDescending(x => x.ContactEmail)
                    : q => q.OrderBy(x => x.ContactEmail),

                "contactphone" => isDescending
                    ? q => q.OrderByDescending(x => x.ContactPhone)
                    : q => q.OrderBy(x => x.ContactPhone),

                "createdat" => isDescending
                    ? q => q.OrderByDescending(x => x.CreatedAt)
                    : q => q.OrderBy(x => x.CreatedAt),

                _ => isDescending
                    ? q => q.OrderByDescending(x => x.Id)
                    : q => q.OrderBy(x => x.Id)
            };
        }
    }
}