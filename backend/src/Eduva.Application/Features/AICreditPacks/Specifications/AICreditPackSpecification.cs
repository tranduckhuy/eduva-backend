using Eduva.Application.Common.Specifications;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
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
            var searchTerm = param.SearchTerm?.ToLower() ?? "";

            return pack =>
                (param.ActiveOnly == null ||
                 (param.ActiveOnly.Value && pack.Status == EntityStatus.Active) ||
                 (!param.ActiveOnly.Value && pack.Status != EntityStatus.Active)) &&
                (string.IsNullOrWhiteSpace(param.SearchTerm) ||
                 pack.Name.ToLower().Contains(searchTerm));
        }

        private static Func<IQueryable<AICreditPack>, IOrderedQueryable<AICreditPack>>? BuildOrderBy(AICreditPackSpecParam param)
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
                    ? q => q.OrderByDescending(x => x.Price)
                    : q => q.OrderBy(x => x.Price),
                _ => isDescending
                    ? q => q.OrderByDescending(x => x.CreatedAt)
                    : q => q.OrderBy(x => x.CreatedAt)
            };
        }
    }
}