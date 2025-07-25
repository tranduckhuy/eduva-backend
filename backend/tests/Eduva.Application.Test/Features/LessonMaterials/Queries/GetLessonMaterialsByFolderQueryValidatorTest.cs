using Eduva.Application.Features.LessonMaterials.DTOs;
using Eduva.Application.Features.LessonMaterials.Queries.GetLessonMaterialsByFolder;
using Eduva.Application.Features.LessonMaterials.Queries.Validators;
using Eduva.Domain.Enums;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.LessonMaterials.Queries
{
    [TestFixture]
    public class GetLessonMaterialsByFolderQueryValidatorTest
    {
        private GetLessonMaterialsByFolderQueryValidator _validator = null!;

        [SetUp]
        public void Setup()
        {
            _validator = new GetLessonMaterialsByFolderQueryValidator();
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
        public void Should_Have_Error_When_UserRoles_Is_Empty()
        {
            var query = new GetLessonMaterialsByFolderQuery(Guid.NewGuid(), Guid.NewGuid(), 1, new List<string>());
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(q => q.UserRoles);
        }

        [Test]
        public void Should_Have_Error_When_SortDirection_Is_Invalid()
        {
            var filterOptions = new LessonMaterialFilterOptions { SortDirection = "invalid" };
            var query = new GetLessonMaterialsByFolderQuery(Guid.NewGuid(), Guid.NewGuid(), 1, new List<string> { "Teacher" }, filterOptions);
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor("FilterOptions.SortDirection");
        }

        [Test]
        public void Should_Have_Error_When_SortBy_Is_Invalid()
        {
            var filterOptions = new LessonMaterialFilterOptions { SortBy = "invalid" };
            var query = new GetLessonMaterialsByFolderQuery(Guid.NewGuid(), Guid.NewGuid(), 1, new List<string> { "Teacher" }, filterOptions);
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor("FilterOptions.SortBy");
        }

        [Test]
        public void Should_Have_Error_When_LessonStatus_Is_Invalid()
        {
            var filterOptions = new LessonMaterialFilterOptions { LessonStatus = (LessonMaterialStatus)999 };
            var query = new GetLessonMaterialsByFolderQuery(Guid.NewGuid(), Guid.NewGuid(), 1, new List<string> { "Teacher" }, filterOptions);
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor("FilterOptions.LessonStatus");
        }

        [Test]
        public void Should_Have_Error_When_Status_Is_Invalid()
        {
            var filterOptions = new LessonMaterialFilterOptions { Status = (EntityStatus)999 };
            var query = new GetLessonMaterialsByFolderQuery(Guid.NewGuid(), Guid.NewGuid(), 1, new List<string> { "Teacher" }, filterOptions);
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor("FilterOptions.Status");
        }

        [Test]
        public void Should_Not_Have_Error_For_Valid_Query()
        {
            var filterOptions = new LessonMaterialFilterOptions
            {
                SortDirection = "asc",
                SortBy = "title",
                LessonStatus = LessonMaterialStatus.Approved,
                Status = EntityStatus.Active
            };
            var query = new GetLessonMaterialsByFolderQuery(Guid.NewGuid(), Guid.NewGuid(), 1, new List<string> { "Teacher" }, filterOptions);
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}