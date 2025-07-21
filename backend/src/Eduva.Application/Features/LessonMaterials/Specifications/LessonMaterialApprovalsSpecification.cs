using Eduva.Application.Common.Specifications;
using Eduva.Domain.Entities;
using System.Linq.Expressions;

namespace Eduva.Application.Features.LessonMaterials.Specifications
{
    public class LessonMaterialApprovalsSpecification : ISpecification<LessonMaterialApproval>
    {
        public Expression<Func<LessonMaterialApproval, bool>> Criteria { get; private set; }
        public Func<IQueryable<LessonMaterialApproval>, IOrderedQueryable<LessonMaterialApproval>>? OrderBy { get; private set; }
        public List<Expression<Func<LessonMaterialApproval, object>>> Includes { get; private set; } = [];
        public Func<IQueryable<LessonMaterialApproval>, IQueryable<LessonMaterialApproval>>? Selector { get; }
        public int Skip { get; private set; }
        public int Take { get; private set; }

        public LessonMaterialApprovalsSpecification(LessonMaterialApprovalsSpecParam param)
        {
            // Set your filter criteria
            Criteria = x =>
                (!param.LessonMaterialId.HasValue || x.LessonMaterialId == param.LessonMaterialId) &&
                (!param.ApproverId.HasValue || x.ApproverId == param.ApproverId) &&
                (!param.CreatedByUserId.HasValue || x.LessonMaterial.CreatedByUserId == param.CreatedByUserId) &&
                (!param.StatusChangeTo.HasValue || x.StatusChangeTo == param.StatusChangeTo) &&
                (!param.FromDate.HasValue || x.CreatedAt >= param.FromDate) &&
                (!param.ToDate.HasValue || x.CreatedAt <= param.ToDate) &&
                (!param.SchoolId.HasValue || x.LessonMaterial.SchoolId == param.SchoolId) &&
                (string.IsNullOrEmpty(param.SearchTerm) ||
                 x.LessonMaterial.Title.ToLower().Contains(param.SearchTerm.ToLower()) ||
                 (x.Feedback != null && x.Feedback.ToLower().Contains(param.SearchTerm.ToLower())));

            // Add includes
            Includes.Add(x => x.LessonMaterial);
            Includes.Add(x => x.Approver);

            // Handle sorting
            if (!string.IsNullOrEmpty(param.SortBy))
            {
                bool isDescending = param.SortDirection?.Equals("desc", StringComparison.OrdinalIgnoreCase) ?? true;

                OrderBy = param.SortBy.ToLower() switch
                {
                    "date" => isDescending
                        ? q => q.OrderByDescending(x => x.CreatedAt)
                        : q => q.OrderBy(x => x.CreatedAt),

                    "status" => isDescending
                        ? q => q.OrderByDescending(x => x.StatusChangeTo)
                        : q => q.OrderBy(x => x.StatusChangeTo),

                    "material" => isDescending
                        ? q => q.OrderByDescending(x => x.LessonMaterial.Title)
                        : q => q.OrderBy(x => x.LessonMaterial.Title),

                    "approver" => isDescending
                        ? q => q.OrderByDescending(x => x.Approver.FullName)
                        : q => q.OrderBy(x => x.Approver.FullName),

                    _ => isDescending
                        ? q => q.OrderByDescending(x => x.CreatedAt)
                        : q => q.OrderBy(x => x.CreatedAt)
                };
            }
            else
            {
                OrderBy = q => q.OrderByDescending(x => x.CreatedAt);
            }

            // Fix for pagination
            // Ensure PageIndex is at least 1 to avoid negative offset
            int pageIndex = Math.Max(1, param.PageIndex);
            int pageSize = Math.Max(1, param.PageSize); // Also ensure PageSize is at least 1

            Skip = (pageIndex - 1) * pageSize;
            Take = pageSize;
        }
    }
}