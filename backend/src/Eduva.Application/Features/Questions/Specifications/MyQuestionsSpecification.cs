using Eduva.Application.Common.Specifications;
using Eduva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Eduva.Application.Features.Questions.Specifications
{
    public class MyQuestionsSpecification : ISpecification<LessonMaterialQuestion>
    {
        public Expression<Func<LessonMaterialQuestion, bool>> Criteria { get; private set; }
        public Func<IQueryable<LessonMaterialQuestion>, IOrderedQueryable<LessonMaterialQuestion>>? OrderBy { get; private set; }
        public List<Expression<Func<LessonMaterialQuestion, object>>> Includes { get; private set; } = [];
        public Func<IQueryable<LessonMaterialQuestion>, IQueryable<LessonMaterialQuestion>>? Selector => null;
        public int Skip { get; private set; }
        public int Take { get; private set; }

        public MyQuestionsSpecification(MyQuestionsSpecParam param, Guid userId, int? userSchoolId)
        {
            Criteria = BuildCriteria(param, userId, userSchoolId);
            OrderBy = BuildOrderBy(param);
            Skip = (param.PageIndex - 1) * param.PageSize;
            Take = param.PageSize;

            Includes.Add(q => q.CreatedByUser);
            Includes.Add(q => q.LessonMaterial);
            Includes.Add(q => q.Comments);
        }

        private static Expression<Func<LessonMaterialQuestion, bool>> BuildCriteria(MyQuestionsSpecParam param, Guid userId, int? userSchoolId)
        {
            var loweredSearch = param.SearchTerm?.ToLower() ?? string.Empty;

            return q =>
                q.CreatedByUserId == userId &&
                (userSchoolId == null || q.LessonMaterial.SchoolId == userSchoolId) &&
                (string.IsNullOrWhiteSpace(loweredSearch) ||
                    EF.Functions.Like(q.Title.ToLower(), $"%{loweredSearch}%") ||
                    EF.Functions.Like((q.LessonMaterial.Title ?? "").ToLower(), $"%{loweredSearch}%"));
        }

        private static Func<IQueryable<LessonMaterialQuestion>, IOrderedQueryable<LessonMaterialQuestion>>? BuildOrderBy(MyQuestionsSpecParam param)
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

                "lessonmaterial" => isDesc
                    ? q => q.OrderByDescending(x => x.LessonMaterial.Title)
                    : q => q.OrderBy(x => x.LessonMaterial.Title),

                "createdat" => isDesc
                    ? q => q.OrderByDescending(x => x.CreatedAt)
                    : q => q.OrderBy(x => x.CreatedAt),

                _ => q => q.OrderByDescending(x => x.CreatedAt)
            };
        }
    }
}