using Eduva.Application.Common.Specifications;
using Eduva.Domain.Entities;
using System.Linq.Expressions;

namespace Eduva.Application.Features.LessonMaterials.Specifications
{
    public class LessonMaterialSpecification : ISpecification<LessonMaterial>
    {
        public Expression<Func<LessonMaterial, bool>> Criteria { get; private set; }

        public Func<IQueryable<LessonMaterial>, IOrderedQueryable<LessonMaterial>>? OrderBy { get; private set; }

        public List<Expression<Func<LessonMaterial, object>>> Includes { get; private set; } = [];

        public Func<IQueryable<LessonMaterial>, IQueryable<LessonMaterial>>? Selector { get; private set; }

        public int Skip { get; private set; }

        public int Take { get; private set; }

        public LessonMaterialSpecification(LessonMaterialSpecParam param)
        {
            Criteria = lm =>
                (!param.SchoolId.HasValue || lm.SchoolId == param.SchoolId) &&
                (!param.CreatedByUserId.HasValue || lm.CreatedBy == param.CreatedByUserId) &&
                (string.IsNullOrEmpty(param.SearchTerm) || lm.Title.ToLower().Contains(param.SearchTerm.ToLower())) &&
                (string.IsNullOrEmpty(param.Tag) || lm.Tag == param.Tag) &&
                (param.ContentTypes == null || !param.ContentTypes.Any() || param.ContentTypes.Contains(lm.ContentType)) &&
                (!param.ClassId.HasValue || lm.FolderLessonMaterials.Any(flm => flm.Folder.ClassId == param.ClassId)) &&
                (!param.FolderId.HasValue || lm.FolderLessonMaterials.Any(flm => flm.FolderId == param.FolderId)) &&
                (!param.LessonStatus.HasValue || lm.LessonStatus == param.LessonStatus) &&
                (!param.Visibility.HasValue || lm.Visibility == param.Visibility);

            Includes.Add(lm => lm.CreatedByUser);

            if (!string.IsNullOrEmpty(param.SortBy))
            {
                bool isDescending = param.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

                OrderBy = param.SortBy.ToLower() switch
                {
                    "title" => isDescending
                        ? q => q.OrderByDescending(lm => lm.Title)
                        : q => q.OrderBy(lm => lm.Title),

                    _ => isDescending
                        ? q => q.OrderByDescending(lm => lm.CreatedAt)
                        : q => q.OrderBy(lm => lm.CreatedAt)
                };
            }

            Skip = (param.PageIndex - 1) * param.PageSize;
            Take = param.PageSize;
        }
    }
}
