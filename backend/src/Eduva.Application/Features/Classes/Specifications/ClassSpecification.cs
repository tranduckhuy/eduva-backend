using Eduva.Application.Common.Specifications;
using Eduva.Domain.Entities;
using System.Linq.Expressions;

namespace Eduva.Application.Features.Classes.Specifications
{
    public class ClassSpecification : ISpecification<Classroom>
    {
        public Expression<Func<Classroom, bool>> Criteria { get; private set; }

        public Func<IQueryable<Classroom>, IOrderedQueryable<Classroom>>? OrderBy { get; private set; }

        public List<Expression<Func<Classroom, object>>> Includes { get; private set; } = [];

        public Func<IQueryable<Classroom>, IQueryable<Classroom>>? Selector { get; }

        public int Skip { get; private set; }

        public int Take { get; private set; }

        public ClassSpecification(ClassSpecParam param)
        {
            Criteria = c =>
                (!param.SchoolId.HasValue || c.SchoolId == param.SchoolId) &&
                (!param.TeacherId.HasValue || c.TeacherId == param.TeacherId) &&
                (!param.ClassId.HasValue || c.Id == param.ClassId) &&
                (!param.Status.HasValue || c.Status == param.Status) &&
                (string.IsNullOrEmpty(param.SearchTerm) || c.Name.ToLower().Contains(param.SearchTerm.ToLower()) ||
                                                           (c.ClassCode != null && c.ClassCode.ToLower().Contains(param.SearchTerm.ToLower())));

            Includes.Add(c => c.Teacher);
            Includes.Add(c => c.School);

            if (!string.IsNullOrEmpty(param.SortBy))
            {
                bool isDescending = !string.IsNullOrEmpty(param.SortDirection) && 
                   param.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

                OrderBy = param.SortBy.ToLower() switch
                {
                    "name" => isDescending
                        ? q => q.OrderByDescending(c => c.Name)
                        : q => q.OrderBy(c => c.Name),

                    "lastmodifiedat" => isDescending
                       ? q => q.OrderByDescending(c => c.LastModifiedAt)
                       : q => q.OrderBy(c => c.LastModifiedAt),

                    "createdat" => isDescending
                        ? q => q.OrderByDescending(c => c.CreatedAt)
                        : q => q.OrderBy(c => c.CreatedAt),

                    _ => isDescending
                        ? q => q.OrderByDescending(c => c.CreatedAt)
                        : q => q.OrderBy(c => c.CreatedAt)
                };
            }

            Skip = (param.PageIndex - 1) * param.PageSize;
            Take = param.PageSize;
        }
    }
}
