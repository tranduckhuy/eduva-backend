using Eduva.Application.Common.Specifications;
using Eduva.Domain.Entities;
using System.Linq.Expressions;

namespace Eduva.Application.Features.Folders.Specifications
{
    public class FolderSpecification : ISpecification<Folder>
    {
        public Expression<Func<Folder, bool>> Criteria { get; private set; } = f => true;
        public Func<IQueryable<Folder>, IOrderedQueryable<Folder>>? OrderBy { get; private set; }
        public List<Expression<Func<Folder, object>>> Includes { get; private set; } = [];
        public Func<IQueryable<Folder>, IQueryable<Folder>>? Selector { get; }
        public int Skip { get; private set; }
        public int Take { get; private set; }

        public FolderSpecification(FolderSpecParam param)
        {
            ApplyFilterCriteria(param);
            AddIncludes();
            ApplySorting(param);
            ApplyPaging(param);
        }

        private void ApplyFilterCriteria(FolderSpecParam param)
        {
            // Build base criteria
            if (param.UserId.HasValue)
            {
                Criteria = CombineWithAnd(Criteria, f => f.UserId == param.UserId);
            }

            if (param.ClassId.HasValue)
            {
                Criteria = CombineWithAnd(Criteria, f => f.ClassId == param.ClassId);
            }

            if (param.OwnerType.HasValue)
            {
                Criteria = CombineWithAnd(Criteria, f => f.OwnerType == param.OwnerType);
            }

            if (param.Status.HasValue)
            {
                Criteria = CombineWithAnd(Criteria, f => f.Status == param.Status);
            }

            ApplySearchTermFilter(param);
            ApplyNameFilter(param);
        }

        private void ApplySearchTermFilter(FolderSpecParam param)
        {
            if (!string.IsNullOrEmpty(param.SearchTerm))
            {
                var searchTerm = param.SearchTerm.ToLower();
                Criteria = CombineWithAnd(Criteria, f => f.Name.ToLower().Contains(searchTerm));
            }
        }

        private void ApplyNameFilter(FolderSpecParam param)
        {
            if (!string.IsNullOrEmpty(param.Name))
            {
                Criteria = CombineWithAnd(Criteria, f =>
                    f.Name.ToLower().Contains(param.Name.ToLower()));
            }
        }
        private void AddIncludes()
        {
            Includes.Add(f => f.User!);
            Includes.Add(f => f.Class!);
        }

        private void ApplySorting(FolderSpecParam param)
        {
            if (string.IsNullOrEmpty(param.SortBy))
            {
                // Default sort by order
                OrderBy = q => q.OrderBy(f => f.Order);
                return;
            }

            bool isDescending = param.SortDirection?.Equals("desc", StringComparison.OrdinalIgnoreCase) ?? false;
            OrderBy = GetSortOrderFunction(param.SortBy.ToLower(), isDescending);
        }

        private static Func<IQueryable<Folder>, IOrderedQueryable<Folder>> GetSortOrderFunction(string sortBy, bool isDescending)
        {
            return sortBy switch
            {
                "name" => GetOrderByFunc(f => f.Name, isDescending),
                "order" => GetOrderByFunc(f => f.Order, isDescending),
                "createdat" => GetOrderByFunc(f => f.CreatedAt, isDescending),
                "lastmodifiedat" => GetOrderByFunc(f => f.LastModifiedAt, isDescending),
                _ => GetOrderByFunc(f => f.Order, isDescending)
            };
        }

        private void ApplyPaging(FolderSpecParam param)
        {
            if (param.IsPagingEnabled)
            {
                Skip = (param.PageIndex - 1) * param.PageSize;
                Take = param.PageSize;
            }
            else
            {
                Skip = 0;
                Take = int.MaxValue;
            }
        }


        private static Expression<Func<Folder, bool>> CombineWithAnd(
            Expression<Func<Folder, bool>> left,
            Expression<Func<Folder, bool>> right)
        {
            var param = Expression.Parameter(typeof(Folder), "f");
            var body = Expression.AndAlso(
                Expression.Invoke(left, param),
                Expression.Invoke(right, param)
            );
            return Expression.Lambda<Func<Folder, bool>>(body, param);
        }

        private static Func<IQueryable<Folder>, IOrderedQueryable<Folder>> GetOrderByFunc<TKey>(
            Expression<Func<Folder, TKey>> keySelector, bool isDescending)
        {
            return isDescending
                ? q => q.OrderByDescending(keySelector)
                : q => q.OrderBy(keySelector);
        }
    }
}
