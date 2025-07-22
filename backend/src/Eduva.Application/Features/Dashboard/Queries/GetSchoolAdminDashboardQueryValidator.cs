using Eduva.Domain.Enums;
using FluentValidation;

namespace Eduva.Application.Features.Dashboard.Queries
{
    public class GetSchoolAdminDashboardQueryValidator : AbstractValidator<GetSchoolAdminDashboardQuery>
    {
        public GetSchoolAdminDashboardQueryValidator()
        {
            RuleFor(x => x.SchoolAdminId)
                .NotEmpty()
                .WithMessage("SchoolAdminId is required");

            RuleFor(x => x.LessonActivityPeriod)
                .Must(period => period == PeriodType.Week || period == PeriodType.Month)
                .WithMessage("Lesson activity period must be 'Week' or 'Month'");

            RuleFor(x => x.LessonStatusPeriod)
                .Must(period => period == PeriodType.Week || period == PeriodType.Month)
                .WithMessage("Lesson status period must be 'Week' or 'Month'");

            RuleFor(x => x.ContentTypePeriod)
                .Must(period => !period.HasValue || period == PeriodType.Week || period == PeriodType.Month)
                .WithMessage("Content type period must be all-time, 'Week' or 'Month'");

            RuleFor(x => x.ReviewLessonsLimit)
                .GreaterThan(0)
                .LessThanOrEqualTo(10)
                .WithMessage("Review lessons limit must be between 1 and 10");

            RuleFor(x => x.TopTeachersLimit)
                .GreaterThan(0)
                .LessThanOrEqualTo(10)
                .WithMessage("Top teachers limit must be between 1 and 10");

            RuleFor(x => x)
                .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.StartDate <= x.EndDate)
                .WithMessage("Start date must be less than or equal to end date");
        }
    }
}