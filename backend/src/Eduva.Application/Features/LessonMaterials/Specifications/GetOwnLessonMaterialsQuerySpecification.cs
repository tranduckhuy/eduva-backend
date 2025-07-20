using Eduva.Application.Common.Specifications;
using Eduva.Domain.Entities;
using System.Linq.Expressions;

namespace Eduva.Application.Features.LessonMaterials.Specifications
{
    public class GetOwnLessonMaterialsQuerySpecification : ISpecification<LessonMaterial>
    {
        public Expression<Func<LessonMaterial, bool>> Criteria { get; private set; }
        public Func<IQueryable<LessonMaterial>, IOrderedQueryable<LessonMaterial>>? OrderBy { get; private set; }
        public List<Expression<Func<LessonMaterial, object>>> Includes { get; private set; } = [];
        public Func<IQueryable<LessonMaterial>, IQueryable<LessonMaterial>>? Selector { get; private set; }
        public int Skip { get; private set; }
        public int Take { get; private set; }

        public GetOwnLessonMaterialsQuerySpecification(LessonMaterialSpecParam param, Guid userId)
        {
            Criteria = lm =>
                (lm.CreatedByUserId == userId) &&
                (string.IsNullOrEmpty(param.SearchTerm) || lm.Title.ToLower().Contains(param.SearchTerm.ToLower())) &&
                (!param.ContentType.HasValue || lm.ContentType == param.ContentType) &&
                (!param.LessonStatus.HasValue || lm.LessonStatus == param.LessonStatus) &&
                (!param.Visibility.HasValue || lm.Visibility == param.Visibility) &&
                (lm.Status == param.EntityStatus);

            Includes.Add(lm => lm.CreatedByUser);
            Includes.Add(lm => lm.FolderLessonMaterials);

            if (!string.IsNullOrEmpty(param.SortBy))
            {
                bool isDescending = param.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

                OrderBy = param.SortBy.ToLower() switch
                {
                    "title" => isDescending
                        ? q => q.OrderByDescending(lm => lm.Title)
                        : q => q.OrderBy(lm => lm.Title),

                    "createdat" => isDescending
                        ? q => q.OrderByDescending(lm => lm.CreatedAt)
                        : q => q.OrderBy(lm => lm.CreatedAt),

                    "lastmodifiedat" => isDescending
                        ? q => q.OrderByDescending(lm => lm.LastModifiedAt)
                        : q => q.OrderBy(lm => lm.LastModifiedAt),

                    "createdby" => isDescending
                        ? q => q.OrderByDescending(lm => lm.CreatedByUser.FullName)
                        : q => q.OrderBy(lm => lm.CreatedByUser.FullName),

                    _ => isDescending
                        ? q => q.OrderByDescending(lm => lm.CreatedAt)
                        : q => q.OrderBy(lm => lm.CreatedAt)
                };
            }
            else
            {
                OrderBy = q => q.OrderByDescending(lm => lm.CreatedAt);
            }


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
    }
}
