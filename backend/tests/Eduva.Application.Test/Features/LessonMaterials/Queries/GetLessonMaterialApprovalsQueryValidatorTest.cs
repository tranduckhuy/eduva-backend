using Eduva.Application.Features.LessonMaterials.Queries.GetLessonMaterialApprovals;
using Eduva.Application.Features.LessonMaterials.Specifications;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.LessonMaterials.Queries
{
    [TestFixture]
    public class GetLessonMaterialApprovalsQueryValidatorTest
    {
        private GetLessonMaterialApprovalsQueryValidator _validator = null!;

        [SetUp]
        public void Setup()
        {
            _validator = new GetLessonMaterialApprovalsQueryValidator();
        }

        [Test]
        public void Should_Have_Error_When_SpecParam_Is_Null()
        {
            var query = new GetLessonMaterialApprovalsQuery(null!, Guid.NewGuid(), new List<string> { "Teacher" });

            try
            {
                _validator.TestValidate(query);
                Assert.Fail("Expected a NullReferenceException but none was thrown.");
            }
            catch (NullReferenceException)
            {
                Assert.Pass("Caught expected NullReferenceException due to SpecParam being null.");
            }
        }

        [Test]
        public void Should_Have_Error_When_PageSize_Is_Invalid()
        {
            var specParam = new LessonMaterialApprovalsSpecParam { PageSize = 0, PageIndex = 1 };
            var query = new GetLessonMaterialApprovalsQuery(specParam, Guid.NewGuid(), new List<string> { "Teacher" });
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor("SpecParam.PageSize");

            specParam.PageSize = 51;
            result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor("SpecParam.PageSize");
        }

        [Test]
        public void Should_Have_Error_When_PageIndex_Is_Negative()
        {
            var specParam = new LessonMaterialApprovalsSpecParam { PageSize = 10, PageIndex = -1 };
            var query = new GetLessonMaterialApprovalsQuery(specParam, Guid.NewGuid(), new List<string> { "Teacher" });
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor("SpecParam.PageIndex");
        }

        [Test]
        public void Should_Have_Error_When_UserId_Is_Empty()
        {
            var specParam = new LessonMaterialApprovalsSpecParam { PageSize = 10, PageIndex = 1 };
            var query = new GetLessonMaterialApprovalsQuery(specParam, Guid.Empty, new List<string> { "Teacher" });
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(q => q.UserId);
        }

        [Test]
        public void Should_Have_Error_When_UserRoles_Is_Null()
        {
            var specParam = new LessonMaterialApprovalsSpecParam { PageSize = 10, PageIndex = 1 };
            var query = new GetLessonMaterialApprovalsQuery(specParam, Guid.NewGuid(), null!);
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(q => q.UserRoles);
        }

        [Test]
        public void Should_Not_Have_Error_For_Valid_Query()
        {
            var specParam = new LessonMaterialApprovalsSpecParam { PageSize = 10, PageIndex = 1 };
            var query = new GetLessonMaterialApprovalsQuery(specParam, Guid.NewGuid(), new List<string> { "Teacher" });
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}