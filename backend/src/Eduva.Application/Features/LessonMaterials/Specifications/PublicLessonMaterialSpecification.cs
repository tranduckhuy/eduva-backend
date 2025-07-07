using Eduva.Application.Common.Specifications;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using System.Linq.Expressions;

namespace Eduva.Application.Features.LessonMaterials.Specifications
{
    public class PublicLessonMaterialSpecification : ISpecification<LessonMaterial>
    {
        public Expression<Func<LessonMaterial, bool>> Criteria { get; private set; }

        public Func<IQueryable<LessonMaterial>, IOrderedQueryable<LessonMaterial>>? OrderBy { get; private set; }

        public List<Expression<Func<LessonMaterial, object>>> Includes { get; private set; } = [];

        public Func<IQueryable<LessonMaterial>, IQueryable<LessonMaterial>>? Selector { get; private set; }

        public int Skip { get; private set; }

        public int Take { get; private set; }

        public PublicLessonMaterialSpecification(LessonMaterialSpecParam param)
        {
            Criteria = lm =>
                (!param.CreatedByUserId.HasValue || lm.CreatedByUserId == param.CreatedByUserId) &&
                (string.IsNullOrEmpty(param.SearchTerm) || lm.Title.ToLower().Contains(param.SearchTerm.ToLower())) &&
                (string.IsNullOrEmpty(param.Tag) || lm.Tag == param.Tag) &&
                (!param.ContentType.HasValue || lm.ContentType == param.ContentType) &&
                (lm.LessonStatus == LessonMaterialStatus.Approved) &&
                (lm.Visibility == LessonMaterialVisibility.School) &&
                (lm.Status == param.EntityStatus);

            Includes.Add(lm => lm.CreatedByUser);

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

                    _ => isDescending
                        ? q => q.OrderByDescending(lm => lm.LastModifiedAt)
                        : q => q.OrderBy(lm => lm.LastModifiedAt),
                };
            }

            Skip = (param.PageIndex - 1) * param.PageSize;
            Take = param.PageSize;
        }
    }
}
