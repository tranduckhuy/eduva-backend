using Eduva.Application.Features.LessonMaterials.Queries.GetLessonMaterialsByFolder;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.LessonMaterials.Queries
{
    [TestFixture]
    public class GetLessonMaterialsByFolderValidatorTest
    {
        private GetLessonMaterialsByFolderValidator _validator = null!;

        [SetUp]
        public void Setup()
        {
            _validator = new GetLessonMaterialsByFolderValidator();
        }

        [Test]
        public void Should_Have_Error_When_FolderId_Is_Empty()
        {
            var query = new GetLessonMaterialsByFolderQuery(Guid.Empty, Guid.NewGuid(), 1, new List<string> { "Teacher" });
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(q => q.FolderId);
        }

        [Test]
        public void Should_Have_Error_When_UserId_Is_Empty()
        {
            var query = new GetLessonMaterialsByFolderQuery(Guid.NewGuid(), Guid.Empty, 1, new List<string> { "Teacher" });
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(q => q.UserId);
        }

        [Test]
        public void Should_Have_Error_When_UserRoles_Is_Null()
        {
            var query = new GetLessonMaterialsByFolderQuery(Guid.NewGuid(), Guid.NewGuid(), 1, null!);
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(q => q.UserRoles);
        }

        [Test]
        public void Should_Have_Error_When_UserRoles_Is_Empty()
        {
            var query = new GetLessonMaterialsByFolderQuery(Guid.NewGuid(), Guid.NewGuid(), 1, new List<string>());
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(q => q.UserRoles);
        }

        [Test]
        public void Should_Not_Have_Error_For_Valid_Query()
        {
            var query = new GetLessonMaterialsByFolderQuery(Guid.NewGuid(), Guid.NewGuid(), 1, new List<string> { "Teacher" });
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}