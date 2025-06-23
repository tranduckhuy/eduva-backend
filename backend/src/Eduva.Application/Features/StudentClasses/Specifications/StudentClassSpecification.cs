using Eduva.Application.Common.Specifications;
using Eduva.Domain.Entities;
using System.Linq.Expressions;

namespace Eduva.Application.Features.StudentClasses.Specifications
{
    public class StudentClassSpecification : ISpecification<StudentClass>
    {
        public Expression<Func<StudentClass, bool>> Criteria { get; private set; } = sc => true;

        public Func<IQueryable<StudentClass>, IOrderedQueryable<StudentClass>>? OrderBy { get; private set; }

        public List<Expression<Func<StudentClass, object>>> Includes { get; private set; } = [];

        public Func<IQueryable<StudentClass>, IQueryable<StudentClass>>? Selector { get; }

        public int Skip { get; private set; }

        public int Take { get; private set; }

        public StudentClassSpecification(StudentClassSpecParam param)
        {
            ApplyFilterCriteria(param);
            AddIncludes();
            ApplySorting(param);
            ApplyPaging(param);
        }

        private void ApplyFilterCriteria(StudentClassSpecParam param)
        {
            // Start with the base criteria - student ID
            Criteria = sc => sc.StudentId == param.StudentId;

            // Then add each filter condition separately
            ApplySearchTermFilter(param);
            ApplyClassNameFilter(param);
            ApplyTeacherNameFilter(param);
            ApplySchoolNameFilter(param);
            ApplyClassCodeFilter(param);
            ApplyStatusFilter(param);
        }

        private void ApplySearchTermFilter(StudentClassSpecParam param)
        {
            if (!string.IsNullOrEmpty(param.SearchTerm))
            {
                var searchTerm = param.SearchTerm.ToLower();
                Criteria = CombineWithAnd(Criteria, sc =>
                    sc.Class.Name.ToLower().Contains(searchTerm) ||
                    (sc.Class.ClassCode != null && sc.Class.ClassCode.ToLower().Contains(searchTerm)) ||
                    (sc.Class.Teacher != null && sc.Class.Teacher.FullName != null &&
                     sc.Class.Teacher.FullName.ToLower().Contains(searchTerm)) ||
                    (sc.Class.School != null && sc.Class.School.Name.ToLower().Contains(searchTerm)));
            }
        }
        private void ApplyClassNameFilter(StudentClassSpecParam param)
        {
            if (!string.IsNullOrEmpty(param.ClassName))
            {
                Criteria = CombineWithAnd(Criteria, sc =>
                    sc.Class.Name.ToLower().Contains(param.ClassName.ToLower()));
            }
        }

        private void ApplyTeacherNameFilter(StudentClassSpecParam param)
        {
            if (!string.IsNullOrEmpty(param.TeacherName))
            {
                Criteria = CombineWithAnd(Criteria, sc =>
                    sc.Class.Teacher != null &&
                    sc.Class.Teacher.FullName != null &&
                    sc.Class.Teacher.FullName.ToLower().Contains(param.TeacherName.ToLower()));
            }
        }
        private void ApplySchoolNameFilter(StudentClassSpecParam param)
        {
            if (!string.IsNullOrEmpty(param.SchoolName))
            {
                Criteria = CombineWithAnd(Criteria, sc =>
                    sc.Class.School != null &&
                    sc.Class.School.Name.ToLower().Contains(param.SchoolName.ToLower()));
            }
        }

        private void ApplyClassCodeFilter(StudentClassSpecParam param)
        {
            if (!string.IsNullOrEmpty(param.ClassCode))
            {
                Criteria = CombineWithAnd(Criteria, sc =>
                    sc.Class.ClassCode != null &&
                    sc.Class.ClassCode.ToLower().Contains(param.ClassCode.ToLower()));
            }
        }

        private void ApplyStatusFilter(StudentClassSpecParam param)
        {
            if (param.ClassStatus.HasValue)
            {
                Criteria = CombineWithAnd(Criteria, sc => sc.Class.Status == param.ClassStatus);
            }
        }

        private void AddIncludes()
        {
            Includes.Add(sc => sc.Class);
            Includes.Add(sc => sc.Class.Teacher);
            Includes.Add(sc => sc.Class.School);
        }

        private void ApplySorting(StudentClassSpecParam param)
        {
            if (string.IsNullOrEmpty(param.SortBy))
            {
                // Default sort by enrollment date descending (most recent first)
                OrderBy = q => q.OrderByDescending(sc => sc.EnrolledAt);
                return;
            }

            bool isDescending = param.SortDirection?.Equals("desc", StringComparison.OrdinalIgnoreCase) ?? false;
            OrderBy = GetSortOrderFunction(param.SortBy.ToLower(), isDescending);
        }

        private static Func<IQueryable<StudentClass>, IOrderedQueryable<StudentClass>> GetSortOrderFunction(string sortBy, bool isDescending)
        {
            return sortBy switch
            {
                "classname" => GetOrderByFunc(sc => sc.Class.Name, isDescending),
                "schoolname" => GetOrderByFunc(sc => sc.Class.School != null ? sc.Class.School.Name : string.Empty, isDescending),
                "teachername" => GetOrderByFunc(sc => sc.Class.Teacher != null ? sc.Class.Teacher.FullName ?? string.Empty : string.Empty, isDescending),
                "classcode" => GetOrderByFunc(sc => sc.Class.ClassCode ?? string.Empty, isDescending),
                "status" => GetOrderByFunc(sc => sc.Class.Status, isDescending),
                _ => GetOrderByFunc(sc => sc.EnrolledAt, isDescending)
            };
        }

        private void ApplyPaging(StudentClassSpecParam param)
        {
            Skip = (param.PageIndex - 1) * param.PageSize;
            Take = param.PageSize;
        }
        private static Expression<Func<StudentClass, bool>> CombineWithAnd(
            Expression<Func<StudentClass, bool>> left,
            Expression<Func<StudentClass, bool>> right)
        {
            var param = Expression.Parameter(typeof(StudentClass), "sc");
            var body = Expression.AndAlso(
                Expression.Invoke(left, param),
                Expression.Invoke(right, param)
            );
            return Expression.Lambda<Func<StudentClass, bool>>(body, param);
        }

        private static Func<IQueryable<StudentClass>, IOrderedQueryable<StudentClass>> GetOrderByFunc<TKey>(
            Expression<Func<StudentClass, TKey>> keySelector, bool isDescending)
        {
            return isDescending
                ? q => q.OrderByDescending(keySelector)
                : q => q.OrderBy(keySelector);
        }
    }
}
