using Eduva.Application.Common.Specifications;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Eduva.Application.Features.Questions.Specifications
{
    public class QuestionsByLessonSpecification : ISpecification<LessonMaterialQuestion>
    {
        public Expression<Func<LessonMaterialQuestion, bool>> Criteria { get; private set; }
        public Func<IQueryable<LessonMaterialQuestion>, IOrderedQueryable<LessonMaterialQuestion>>? OrderBy { get; private set; }
        public List<Expression<Func<LessonMaterialQuestion, object>>> Includes { get; private set; } = [];
        public Func<IQueryable<LessonMaterialQuestion>, IQueryable<LessonMaterialQuestion>>? Selector => null;
        public int Skip { get; private set; }
        public int Take { get; private set; }

        public QuestionsByLessonSpecification(QuestionsByLessonSpecParam param, Guid lessonMaterialId, Guid currentUserId, int? userSchoolId, string userRole)
        {
            Criteria = BuildCriteria(param, lessonMaterialId, currentUserId, userSchoolId, userRole);
            OrderBy = BuildOrderBy(param);
            Skip = (param.PageIndex - 1) * param.PageSize;
            Take = param.PageSize;

            Includes.Add(q => q.CreatedByUser);
            Includes.Add(q => q.LessonMaterial);
            Includes.Add(q => q.Comments);
        }

        private static Expression<Func<LessonMaterialQuestion, bool>> BuildCriteria(QuestionsByLessonSpecParam param, Guid lessonMaterialId, Guid currentUserId, int? userSchoolId, string userRole)
        {
            var loweredSearch = param.SearchTerm?.ToLower() ?? string.Empty;

            return q =>
                q.LessonMaterialId == lessonMaterialId &&
                (userSchoolId == null || q.LessonMaterial.SchoolId == userSchoolId) &&

                (userRole == nameof(Role.SchoolAdmin) || userRole == nameof(Role.SystemAdmin) ||
                 q.CreatedByUserId != currentUserId) &&
                (string.IsNullOrWhiteSpace(loweredSearch) ||
                    EF.Functions.Like(q.Title.ToLower(), $"%{loweredSearch}%") ||
                    EF.Functions.Like((q.CreatedByUser.FullName ?? "").ToLower(), $"%{loweredSearch}%"));
        }

        private static Func<IQueryable<LessonMaterialQuestion>, IOrderedQueryable<LessonMaterialQuestion>>? BuildOrderBy(QuestionsByLessonSpecParam param)
        {
            if (string.IsNullOrWhiteSpace(param.SortBy))
                return q => q.OrderByDescending(x => x.CreatedAt);

            bool isDesc = param.SortDirection.ToLower() == "desc";
            string sort = param.SortBy.ToLower();

            return sort switch
            {
                "title" => isDesc
                    ? q => q.OrderByDescending(x => x.Title)
                    : q => q.OrderBy(x => x.Title),

                "name" => isDesc
                    ? q => q.OrderByDescending(x => x.CreatedByUser.FullName)
                    : q => q.OrderBy(x => x.CreatedByUser.FullName),

                "createdat" => isDesc
                    ? q => q.OrderByDescending(x => x.CreatedAt)
                    : q => q.OrderBy(x => x.CreatedAt),

                _ => q => q.OrderByDescending(x => x.CreatedAt)
            };
        }
    }
}