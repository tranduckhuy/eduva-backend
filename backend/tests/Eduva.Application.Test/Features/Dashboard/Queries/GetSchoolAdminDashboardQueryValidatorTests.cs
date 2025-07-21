using Eduva.Application.Features.Dashboard.Queries;
using Eduva.Domain.Enums;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.Dashboard.Queries
{
    [TestFixture]
    public class GetSchoolAdminDashboardQueryValidatorTests
    {
        private GetSchoolAdminDashboardQueryValidator _validator = null!;

        #region Setup

        [SetUp]
        public void Setup()
        {
            _validator = new GetSchoolAdminDashboardQueryValidator();
        }

        #endregion

        #region Tests

        [Test]
        public void Should_Have_Error_When_SchoolAdminId_Is_Empty()
        {
            var query = new GetSchoolAdminDashboardQuery { SchoolAdminId = Guid.Empty };
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.SchoolAdminId)
                  .WithErrorMessage("SchoolAdminId is required");
        }

        [TestCase(PeriodType.Day)]
        [TestCase(PeriodType.Year)]
        public void Should_Have_Error_When_LessonActivityPeriod_Is_Invalid(PeriodType period)
        {
            var query = new GetSchoolAdminDashboardQuery { LessonActivityPeriod = period };
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.LessonActivityPeriod)
                  .WithErrorMessage("Lesson activity period must be 'Week' or 'Month'");
        }

        [TestCase(PeriodType.Week)]
        [TestCase(PeriodType.Month)]
        public void Should_Not_Have_Error_When_LessonActivityPeriod_Is_Valid(PeriodType period)
        {
            var query = new GetSchoolAdminDashboardQuery { LessonActivityPeriod = period };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x.LessonActivityPeriod);
        }

        [TestCase(PeriodType.Day)]
        [TestCase(PeriodType.Year)]
        public void Should_Have_Error_When_LessonStatusPeriod_Is_Invalid(PeriodType period)
        {
            var query = new GetSchoolAdminDashboardQuery { LessonStatusPeriod = period };
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.LessonStatusPeriod)
                  .WithErrorMessage("Lesson status period must be 'Week' or 'Month'");
        }

        [TestCase(PeriodType.Week)]
        [TestCase(PeriodType.Month)]
        public void Should_Not_Have_Error_When_LessonStatusPeriod_Is_Valid(PeriodType period)
        {
            var query = new GetSchoolAdminDashboardQuery { LessonStatusPeriod = period };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x.LessonStatusPeriod);
        }

        [Test]
        public void Should_Not_Have_Error_When_ContentTypePeriod_Is_Null()
        {
            var query = new GetSchoolAdminDashboardQuery { ContentTypePeriod = null };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x.ContentTypePeriod);
        }

        [TestCase(PeriodType.Day)]
        [TestCase(PeriodType.Year)]
        public void Should_Have_Error_When_ContentTypePeriod_Is_Invalid(PeriodType? period)
        {
            var query = new GetSchoolAdminDashboardQuery { ContentTypePeriod = period };
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.ContentTypePeriod)
                  .WithErrorMessage("Content type period must be all-time, 'Week' or 'Month'");
        }

        [TestCase(PeriodType.Week)]
        [TestCase(PeriodType.Month)]
        public void Should_Not_Have_Error_When_ContentTypePeriod_Is_Valid(PeriodType? period)
        {
            var query = new GetSchoolAdminDashboardQuery { ContentTypePeriod = period };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x.ContentTypePeriod);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Should_Have_Error_When_ReviewLessonsLimit_Is_Invalid_Low(int limit)
        {
            var query = new GetSchoolAdminDashboardQuery { ReviewLessonsLimit = limit };
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.ReviewLessonsLimit)
                  .WithErrorMessage("'Review Lessons Limit' must be greater than '0'.");
        }

        [TestCase(11)]
        public void Should_Have_Error_When_ReviewLessonsLimit_Is_Invalid_High(int limit)
        {
            var query = new GetSchoolAdminDashboardQuery { ReviewLessonsLimit = limit };
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.ReviewLessonsLimit)
                  .WithErrorMessage("Review lessons limit must be between 1 and 10");
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Should_Have_Error_When_TopTeachersLimit_Is_Invalid_Low(int limit)
        {
            var query = new GetSchoolAdminDashboardQuery { TopTeachersLimit = limit };
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.TopTeachersLimit)
                  .WithErrorMessage("'Top Teachers Limit' must be greater than '0'.");
        }

        [TestCase(11)]
        public void Should_Have_Error_When_TopTeachersLimit_Is_Invalid_High(int limit)
        {
            var query = new GetSchoolAdminDashboardQuery { TopTeachersLimit = limit };
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.TopTeachersLimit)
                  .WithErrorMessage("Top teachers limit must be between 1 and 10");
        }

        [TestCase(1)]
        [TestCase(10)]
        public void Should_Not_Have_Error_When_ReviewLessonsLimit_Is_Valid(int limit)
        {
            var query = new GetSchoolAdminDashboardQuery { ReviewLessonsLimit = limit };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x.ReviewLessonsLimit);
        }

        [TestCase(1)]
        [TestCase(10)]
        public void Should_Not_Have_Error_When_TopTeachersLimit_Is_Valid(int limit)
        {
            var query = new GetSchoolAdminDashboardQuery { TopTeachersLimit = limit };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x.TopTeachersLimit);
        }

        [Test]
        public void Should_Have_Error_When_StartDate_Greater_Than_EndDate()
        {
            var query = new GetSchoolAdminDashboardQuery
            {
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(-1)
            };
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x)
                  .WithErrorMessage("Start date must be less than or equal to end date");
        }

        [Test]
        public void Should_Not_Have_Error_When_StartDate_Less_Than_Or_Equal_EndDate()
        {
            var now = DateTimeOffset.UtcNow;
            var query = new GetSchoolAdminDashboardQuery
            {
                StartDate = now.AddDays(-1),
                EndDate = now
            };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveValidationErrorFor(x => x);
        }

        [Test]
        public void Should_Not_Have_Error_When_StartDate_Or_EndDate_Is_Null()
        {
            var now = DateTimeOffset.UtcNow;
            var query1 = new GetSchoolAdminDashboardQuery { StartDate = null, EndDate = now };
            var query2 = new GetSchoolAdminDashboardQuery { StartDate = now, EndDate = null };
            var query3 = new GetSchoolAdminDashboardQuery { StartDate = null, EndDate = null };

            Assert.Multiple(() =>
            {
                _validator.TestValidate(query1).ShouldNotHaveValidationErrorFor(x => x);
                _validator.TestValidate(query2).ShouldNotHaveValidationErrorFor(x => x);
                _validator.TestValidate(query3).ShouldNotHaveValidationErrorFor(x => x);
            });
        }

        [Test]
        public void Should_Be_Valid_When_All_Fields_Are_Valid()
        {
            var query = new GetSchoolAdminDashboardQuery
            {
                SchoolAdminId = Guid.NewGuid(),
                LessonActivityPeriod = PeriodType.Week,
                LessonStatusPeriod = PeriodType.Month,
                ContentTypePeriod = PeriodType.Week,
                ReviewLessonsLimit = 5,
                TopTeachersLimit = 5,
                StartDate = DateTimeOffset.UtcNow.AddDays(-10),
                EndDate = DateTimeOffset.UtcNow
            };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion

    }
}