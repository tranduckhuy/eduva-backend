using Eduva.Application.Features.Classes.Queries.GetTeacherClasses;
using Eduva.Application.Features.Classes.Specifications;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.Classes.Queries.GetTeacherClasses
{
    [TestFixture]
    public class GetTeacherClassesValidatorTest
    {
        private GetTeacherClassesValidator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            _validator = new GetTeacherClassesValidator();
        }

        [Test]
        public void Should_Have_Error_When_TeacherId_Is_Empty()
        {
            var query = new GetTeacherClassesQuery(new ClassSpecParam(), Guid.Empty);
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.TeacherId)
                .WithErrorMessage("TeacherId is required");
        }

        [Test]
        public void Should_Have_Error_When_ClassSpecParam_Is_Null()
        {
            var query = new GetTeacherClassesQuery(null!, Guid.NewGuid());
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.ClassSpecParam)
                .WithErrorMessage("ClassSpecParam is required");
        }

        [Test]
        public void Should_Not_Have_Error_When_All_Valid()
        {
            var query = new GetTeacherClassesQuery(new ClassSpecParam(), Guid.NewGuid());
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}