using Eduva.Domain.Enums;
using FluentValidation;

namespace Eduva.Application.Features.Dashboard.Queries
{
    public class GetTeacherDashboardQueryValidator : AbstractValidator<GetTeacherDashboardQuery>
    {
        public GetTeacherDashboardQueryValidator()
        {
            RuleFor(x => x.TeacherId)
                .NotEmpty()
                .WithMessage("TeacherId is required");

            RuleFor(x => x.LessonActivityPeriod)
                .Must(period => period == PeriodType.Week || period == PeriodType.Month)
                .WithMessage("Lesson activity period must be 'Week' or 'Month'");

            RuleFor(x => x.QuestionVolumePeriod)
                .Must(period => period == PeriodType.Week || period == PeriodType.Month)
                .WithMessage("Question volume period must be 'Week' or 'Month'");

            RuleFor(x => x.ContentTypePeriod)
               .Must(period => !period.HasValue || period == PeriodType.Week || period == PeriodType.Month)
               .WithMessage("Content type period must be all-time, 'Week' or 'Month'");

            RuleFor(x => x.ReviewLessonsLimit)
                .GreaterThan(0)
                .LessThanOrEqualTo(10)
                .WithMessage("Review lessons limit must be between 1 and 10");

            RuleFor(x => x.RecentLessonsLimit)
                .GreaterThan(0)
                .LessThanOrEqualTo(10)
                .WithMessage("Recent lessons limit must be between 1 and 10");

            RuleFor(x => x.UnAnswerQuestionsLimit)
                .GreaterThan(0)
                .LessThanOrEqualTo(10)
                .WithMessage("Unanswered questions limit must be between 1 and 10");

            RuleFor(x => x)
                .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.StartDate <= x.EndDate)
                .WithMessage("Start date must be less than or equal to end date");
        }
    }
}