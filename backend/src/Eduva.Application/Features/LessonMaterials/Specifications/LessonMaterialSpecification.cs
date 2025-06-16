using Eduva.Application.Common.Specifications;
using Eduva.Domain.Entities;
using System.Linq.Expressions;

namespace Eduva.Application.Features.LessonMaterials.Specifications
{
    public class LessonMaterialSpecification : ISpecification<LessonMaterial>
    {
        public Expression<Func<LessonMaterial, bool>> Criteria { get; private set; }

        public Func<IQueryable<LessonMaterial>, IOrderedQueryable<LessonMaterial>>? OrderBy { get; private set; }

        public Func<IQueryable<LessonMaterial>, IOrderedQueryable<LessonMaterial>>? OrderByDescending { get; private set; }

        public List<Expression<Func<LessonMaterial, object>>> Includes { get; private set; } = [];

        public Func<IQueryable<LessonMaterial>, IQueryable<LessonMaterial>>? Selector { get; private set; }

        public int Skip { get; private set; }

        public int Take { get; private set; }

        public LessonMaterialSpecification(LessonMaterialSpecParam param)
        {
            Criteria = lm =>
                (!param.SchoolId.HasValue || lm.SchoolId == param.SchoolId) &&
                (!param.CreatedBy.HasValue || lm.CreatedBy == param.CreatedBy) &&
                (string.IsNullOrEmpty(param.SearchTerm) || lm.Title.ToLower().Contains(param.SearchTerm.ToLower())) &&
                (string.IsNullOrEmpty(param.Tag) || lm.Tag == param.Tag) &&
                (!param.ContentType.HasValue || lm.ContentType == param.ContentType) &&
                (!param.LessonStatus.HasValue || lm.LessonStatus == param.LessonStatus) &&
                (!param.Visibility.HasValue || lm.Visibility == param.Visibility);

            Includes.Add(lm => lm.CreatedByUser);

            if (!string.IsNullOrEmpty(param.SortBy))
            {
                OrderBy = param.SortBy switch
                {
                    "title" => q => q.OrderBy(lm => lm.Title),
                    _ => q => q.OrderBy(lm => lm.CreatedAt)
                };
            }

            Skip = (param.PageIndex - 1) * param.PageSize;
            Take = param.PageSize;
        }
    }
}
