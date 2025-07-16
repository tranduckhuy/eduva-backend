using Eduva.Domain.Enums;
using FluentValidation;

namespace Eduva.Application.Features.Dashboard.Queries
{
    public class GetDashboardQueryValidator : AbstractValidator<GetDashboardQuery>
    {
        public GetDashboardQueryValidator()
        {
            RuleFor(x => x.LessonActivityPeriod)
                .Must(period => period == PeriodType.Week || period == PeriodType.Month)
                .WithMessage("Lesson activity period must be 'Week' or 'Month'");

            RuleFor(x => x.UserRegistrationPeriod)
                .Must(period => period == PeriodType.Day || period == PeriodType.Month || period == PeriodType.Year)
                .WithMessage("User registration period must be 'Day', 'Month', or 'Year'");

            RuleFor(x => x.RevenuePeriod)
                .Must(period => period == PeriodType.Month || period == PeriodType.Year)
                .WithMessage("Revenue period must be 'Month' or 'Year'");

            RuleFor(x => x.TopSchoolsCount)
                .GreaterThan(0)
                .LessThanOrEqualTo(20)
                .WithMessage("Top schools count must be between 1 and 20");

            RuleFor(x => x)
                .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.StartDate <= x.EndDate)
                .WithMessage("Start date must be less than or equal to end date");
        }
    }
}