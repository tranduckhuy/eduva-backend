using Eduva.Application.Common.Specifications;
using Eduva.Domain.Entities;
using System.Linq.Expressions;

namespace Eduva.Application.Features.StudentClasses.Specifications
{
    public class StudentClassSpecification : ISpecification<StudentClass>
    {
        public Expression<Func<StudentClass, bool>> Criteria { get; private set; }

        public Func<IQueryable<StudentClass>, IOrderedQueryable<StudentClass>>? OrderBy { get; private set; }

        public List<Expression<Func<StudentClass, object>>> Includes { get; private set; } = [];

        public Func<IQueryable<StudentClass>, IQueryable<StudentClass>>? Selector { get; }

        public int Skip { get; private set; }

        public int Take { get; private set; }

        public StudentClassSpecification(StudentClassSpecParam param)
        {
            Criteria = sc =>
                sc.StudentId == param.StudentId &&
                (string.IsNullOrEmpty(param.SearchTerm) ||
                    sc.Class.Name.ToLower().Contains(param.SearchTerm.ToLower()) ||
                    (sc.Class.ClassCode != null && sc.Class.ClassCode.ToLower().Contains(param.SearchTerm.ToLower())) ||
                    (sc.Class.Teacher != null && sc.Class.Teacher.FullName != null && sc.Class.Teacher.FullName.ToLower().Contains(param.SearchTerm.ToLower())) ||
                    (sc.Class.School != null && sc.Class.School.Name.ToLower().Contains(param.SearchTerm.ToLower()))) &&
                (string.IsNullOrEmpty(param.ClassName) || sc.Class.Name.ToLower().Contains(param.ClassName.ToLower())) &&
                (string.IsNullOrEmpty(param.TeacherName) || (sc.Class.Teacher != null && sc.Class.Teacher.FullName != null &&
                    sc.Class.Teacher.FullName.ToLower().Contains(param.TeacherName.ToLower()))) &&
                (string.IsNullOrEmpty(param.SchoolName) || (sc.Class.School != null &&
                    sc.Class.School.Name.ToLower().Contains(param.SchoolName.ToLower()))) &&
                (string.IsNullOrEmpty(param.ClassCode) || (sc.Class.ClassCode != null &&
                    sc.Class.ClassCode.ToLower().Contains(param.ClassCode.ToLower()))) &&
                (!param.ClassStatus.HasValue || sc.Class.Status == param.ClassStatus);

            // Include related entities
            Includes.Add(sc => sc.Class);
            Includes.Add(sc => sc.Class.Teacher);
            Includes.Add(sc => sc.Class.School);

            if (!string.IsNullOrEmpty(param.SortBy))
            {
                bool isDescending = param.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

                OrderBy = param.SortBy.ToLower() switch
                {
                    "classname" => isDescending
                        ? q => q.OrderByDescending(sc => sc.Class.Name)
                        : q => q.OrderBy(sc => sc.Class.Name),
                    "schoolname" => isDescending
                        ? q => q.OrderByDescending(sc => sc.Class.School != null ? sc.Class.School.Name : string.Empty)
                        : q => q.OrderBy(sc => sc.Class.School != null ? sc.Class.School.Name : string.Empty),
                    "teachername" => isDescending
                        ? q => q.OrderByDescending(sc => sc.Class.Teacher != null ? sc.Class.Teacher.FullName ?? string.Empty : string.Empty)
                        : q => q.OrderBy(sc => sc.Class.Teacher != null ? sc.Class.Teacher.FullName ?? string.Empty : string.Empty),
                    "classcode" => isDescending
                        ? q => q.OrderByDescending(sc => sc.Class.ClassCode ?? string.Empty)
                        : q => q.OrderBy(sc => sc.Class.ClassCode ?? string.Empty),
                    "status" => isDescending
                        ? q => q.OrderByDescending(sc => sc.Class.Status)
                        : q => q.OrderBy(sc => sc.Class.Status),
                    _ => isDescending
                        ? q => q.OrderByDescending(sc => sc.EnrolledAt)
                        : q => q.OrderBy(sc => sc.EnrolledAt)
                };
            }
            else
            {
                // Default sort by enrollment date descending (most recent first)
                OrderBy = q => q.OrderByDescending(sc => sc.EnrolledAt);
            }

            Skip = (param.PageIndex - 1) * param.PageSize;
            Take = param.PageSize;
        }
    }
}
