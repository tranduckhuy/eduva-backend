using Eduva.Application.Common.Specifications;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Eduva.Application.Features.AICreditPacks.Specifications
{
    public class AICreditPackSpecification : ISpecification<AICreditPack>
    {
        public Expression<Func<AICreditPack, bool>> Criteria { get; private set; }
        public Func<IQueryable<AICreditPack>, IOrderedQueryable<AICreditPack>>? OrderBy { get; private set; }
        public List<Expression<Func<AICreditPack, object>>> Includes { get; } = [];
        public Func<IQueryable<AICreditPack>, IQueryable<AICreditPack>>? Selector { get; init; }
        public int Skip { get; private set; }
        public int Take { get; private set; }

        public AICreditPackSpecification(AICreditPackSpecParam param)
        {
            Criteria = BuildCriteria(param);
            OrderBy = BuildOrderBy(param);
            Skip = (param.PageIndex - 1) * param.PageSize;
            Take = param.PageSize;
        }

        private static Expression<Func<AICreditPack, bool>> BuildCriteria(AICreditPackSpecParam param)
        {
            var loweredSearchTerm = param.SearchTerm?.ToLower();

            return pack =>
                 (param.ActiveOnly == null ||
                  (param.ActiveOnly.Value && pack.Status == EntityStatus.Active) ||
                  (!param.ActiveOnly.Value && pack.Status != EntityStatus.Active)) &&
                 (string.IsNullOrWhiteSpace(loweredSearchTerm) ||
                  EF.Functions.Like(pack.Name.ToLower(), $"%{loweredSearchTerm}%"));
        }

        private static Func<IQueryable<AICreditPack>, IOrderedQueryable<AICreditPack>>? BuildOrderBy(AICreditPackSpecParam param)
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

                "price" => isDescending
                    ? q => q.OrderByDescending(x => x.Price)
                    : q => q.OrderBy(x => x.Price),

                "credits" => isDescending
                    ? q => q.OrderByDescending(x => x.Credits)
                    : q => q.OrderBy(x => x.Credits),

                "bonuscredits" => isDescending
                    ? q => q.OrderByDescending(x => x.BonusCredits)
                    : q => q.OrderBy(x => x.BonusCredits),

                _ => isDescending
                    ? q => q.OrderByDescending(x => x.CreatedAt)
                    : q => q.OrderBy(x => x.CreatedAt)
            };
        }
    }
}