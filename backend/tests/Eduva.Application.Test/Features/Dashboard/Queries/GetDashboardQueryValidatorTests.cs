using Eduva.Application.Features.Dashboard.Queries;
using Eduva.Domain.Enums;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.Dashboard.Queries
{
    [TestFixture]
    public class GetDashboardQueryValidatorTests
    {
        private GetDashboardQueryValidator _validator = null!;

        #region Setup

        [SetUp]
        public void Setup()
        {
            _validator = new GetDashboardQueryValidator();
        }

        #endregion

        #region LessonActivityPeriod Tests

        [Test]
        public void Should_Have_Error_When_LessonActivityPeriod_Is_Day()
        {
            var query = new GetDashboardQuery { LessonActivityPeriod = PeriodType.Day };
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.LessonActivityPeriod)
                  .WithErrorMessage("Lesson activity period must be 'Week' or 'Month'");
        }

        [Test]
        public void Should_Have_Error_When_LessonActivityPeriod_Is_Year()
        {
            var query = new GetDashboardQuery { LessonActivityPeriod = PeriodType.Year };
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.LessonActivityPeriod)
                  .WithErrorMessage("Lesson activity period must be 'Week' or 'Month'");
        }

        [Test]
        public void Should_Not_Have_Error_When_LessonActivityPeriod_Is_Week()
        {
            var query = new GetDashboardQuery { LessonActivityPeriod = PeriodType.Week };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x.LessonActivityPeriod);
        }

        [Test]
        public void Should_Not_Have_Error_When_LessonActivityPeriod_Is_Month()
        {
            var query = new GetDashboardQuery { LessonActivityPeriod = PeriodType.Month };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x.LessonActivityPeriod);
        }

        #endregion

        #region UserRegistrationPeriod Tests

        [Test]
        public void Should_Not_Have_Error_When_UserRegistrationPeriod_Is_Day()
        {
            var query = new GetDashboardQuery { UserRegistrationPeriod = PeriodType.Day };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x.UserRegistrationPeriod);
        }

        [Test]
        public void Should_Not_Have_Error_When_UserRegistrationPeriod_Is_Month()
        {
            var query = new GetDashboardQuery { UserRegistrationPeriod = PeriodType.Month };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x.UserRegistrationPeriod);
        }

        [Test]
        public void Should_Not_Have_Error_When_UserRegistrationPeriod_Is_Year()
        {
            var query = new GetDashboardQuery { UserRegistrationPeriod = PeriodType.Year };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x.UserRegistrationPeriod);
        }

        [Test]
        public void Should_Have_Error_When_UserRegistrationPeriod_Is_Week()
        {
            var query = new GetDashboardQuery { UserRegistrationPeriod = PeriodType.Week };
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.UserRegistrationPeriod)
                  .WithErrorMessage("User registration period must be 'Day', 'Month', or 'Year'");
        }

        #endregion

        #region RevenuePeriod Tests

        [Test]
        public void Should_Have_Error_When_RevenuePeriod_Is_Day()
        {
            var query = new GetDashboardQuery { RevenuePeriod = PeriodType.Day };
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.RevenuePeriod)
                  .WithErrorMessage("Revenue period must be 'Month' or 'Year'");
        }

        [Test]
        public void Should_Have_Error_When_RevenuePeriod_Is_Week()
        {
            var query = new GetDashboardQuery { RevenuePeriod = PeriodType.Week };
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.RevenuePeriod)
                  .WithErrorMessage("Revenue period must be 'Month' or 'Year'");
        }

        [Test]
        public void Should_Not_Have_Error_When_RevenuePeriod_Is_Month()
        {
            var query = new GetDashboardQuery { RevenuePeriod = PeriodType.Month };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x.RevenuePeriod);
        }

        [Test]
        public void Should_Not_Have_Error_When_RevenuePeriod_Is_Year()
        {
            var query = new GetDashboardQuery { RevenuePeriod = PeriodType.Year };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x.RevenuePeriod);
        }

        #endregion

        #region TopSchoolsCount Tests

        [Test]
        public void Should_Have_Error_When_TopSchoolsCount_Is_Zero()
        {
            var query = new GetDashboardQuery { TopSchoolsCount = 0 };
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.TopSchoolsCount)
                  .WithErrorMessage("'Top Schools Count' must be greater than '0'.");
        }

        [Test]
        public void Should_Have_Error_When_TopSchoolsCount_Is_Negative()
        {
            var query = new GetDashboardQuery { TopSchoolsCount = -1 };
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.TopSchoolsCount)
                  .WithErrorMessage("'Top Schools Count' must be greater than '0'.");
        }

        [Test]
        public void Should_Have_Error_When_TopSchoolsCount_Is_Greater_Than_20()
        {
            var query = new GetDashboardQuery { TopSchoolsCount = 21 };
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.TopSchoolsCount)
                  .WithErrorMessage("Top schools count must be between 1 and 20");
        }

        [Test]
        public void Should_Not_Have_Error_When_TopSchoolsCount_Is_Valid()
        {
            var query = new GetDashboardQuery { TopSchoolsCount = 10 };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x.TopSchoolsCount);
        }

        [Test]
        public void Should_Not_Have_Error_When_TopSchoolsCount_Is_Boundary_Values()
        {
            var queryMin = new GetDashboardQuery { TopSchoolsCount = 1 };
            var queryMax = new GetDashboardQuery { TopSchoolsCount = 20 };

            var resultMin = _validator.TestValidate(queryMin);
            var resultMax = _validator.TestValidate(queryMax);

            resultMin.ShouldNotHaveValidationErrorFor(x => x.TopSchoolsCount);
            resultMax.ShouldNotHaveValidationErrorFor(x => x.TopSchoolsCount);
        }

        #endregion

        #region Date Validation Tests

        [Test]
        public void Should_Have_Error_When_StartDate_Is_Greater_Than_EndDate()
        {
            var query = new GetDashboardQuery
            {
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(-1)
            };
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x)
                  .WithErrorMessage("Start date must be less than or equal to end date");
        }

        [Test]
        public void Should_Not_Have_Error_When_StartDate_Is_Less_Than_EndDate()
        {
            var query = new GetDashboardQuery
            {
                StartDate = DateTimeOffset.UtcNow.AddDays(-1),
                EndDate = DateTimeOffset.UtcNow
            };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x);
        }

        [Test]
        public void Should_Not_Have_Error_When_StartDate_Equals_EndDate()
        {
            var date = DateTimeOffset.UtcNow;
            var query = new GetDashboardQuery
            {
                StartDate = date,
                EndDate = date
            };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x);
        }

        [Test]
        public void Should_Not_Have_Error_When_StartDate_Is_Null()
        {
            var query = new GetDashboardQuery
            {
                StartDate = null,
                EndDate = DateTimeOffset.UtcNow
            };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x);
        }

        [Test]
        public void Should_Not_Have_Error_When_EndDate_Is_Null()
        {
            var query = new GetDashboardQuery
            {
                StartDate = DateTimeOffset.UtcNow,
                EndDate = null
            };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x);
        }

        [Test]
        public void Should_Not_Have_Error_When_Both_Dates_Are_Null()
        {
            var query = new GetDashboardQuery
            {
                StartDate = null,
                EndDate = null
            };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x);
        }

        #endregion

        #region Complete Validation Tests

        [Test]
        public void Should_Be_Valid_When_All_Fields_Are_Valid()
        {
            var query = new GetDashboardQuery
            {
                StartDate = DateTimeOffset.UtcNow.AddDays(-30),
                EndDate = DateTimeOffset.UtcNow,
                LessonActivityPeriod = PeriodType.Week,
                UserRegistrationPeriod = PeriodType.Day,
                RevenuePeriod = PeriodType.Month,
                TopSchoolsCount = 10
            };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Should_Have_Multiple_Errors_When_Multiple_Fields_Are_Invalid()
        {
            var query = new GetDashboardQuery
            {
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(-1),
                LessonActivityPeriod = PeriodType.Day,
                UserRegistrationPeriod = PeriodType.Week,
                RevenuePeriod = PeriodType.Day,
                TopSchoolsCount = 0
            };
            var result = _validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(x => x.LessonActivityPeriod);
            result.ShouldHaveValidationErrorFor(x => x.UserRegistrationPeriod);
            result.ShouldHaveValidationErrorFor(x => x.RevenuePeriod);
            result.ShouldHaveValidationErrorFor(x => x.TopSchoolsCount);
            result.ShouldHaveValidationErrorFor(x => x);
        }

        #endregion
    }
}