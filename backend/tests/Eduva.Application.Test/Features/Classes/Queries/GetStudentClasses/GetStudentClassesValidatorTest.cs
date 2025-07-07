using Eduva.Application.Common.Specifications;
using Eduva.Application.Features.Classes.Queries.GetStudentClasses;
using Eduva.Application.Features.Classes.Specifications;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.Classes.Queries.GetStudentClasses
{
    [TestFixture]
    public class GetStudentClassesValidatorTest
    {
        private GetStudentClassesValidator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            _validator = new GetStudentClassesValidator();
        }

        [Test]
        public void Should_Have_Error_When_PageIndex_Is_Zero()
        {
            // Arrange
            var specParam = new StudentClassSpecParam { PageIndex = 0, PageSize = 10 };
            var query = new GetStudentClassesQuery(specParam, Guid.NewGuid());

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.StudentClassSpecParam.PageIndex)
                .WithErrorMessage("Page index must be greater than 0");
        }

        [Test]
        public void Should_Have_Error_When_PageIndex_Is_Negative()
        {
            // Arrange
            var specParam = new StudentClassSpecParam { PageIndex = -1, PageSize = 10 };
            var query = new GetStudentClassesQuery(specParam, Guid.NewGuid());

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.StudentClassSpecParam.PageIndex)
                .WithErrorMessage("Page index must be greater than 0");
        }

        [Test]
        public void Should_Have_Error_When_PageSize_Is_Zero()
        {
            // Arrange
            var specParam = new StudentClassSpecParam { PageIndex = 1, PageSize = 0 };
            var query = new GetStudentClassesQuery(specParam, Guid.NewGuid());

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.StudentClassSpecParam.PageSize)
                .WithErrorMessage("Page size must be greater than 0");
        }

        [Test]
        public void Should_Have_Error_When_PageSize_Is_Negative()
        {
            // Arrange
            var specParam = new StudentClassSpecParam { PageIndex = 1, PageSize = -1 };
            var query = new GetStudentClassesQuery(specParam, Guid.NewGuid());

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.StudentClassSpecParam.PageSize)
                .WithErrorMessage("Page size must be greater than 0");
        }

        [Test]
        public void Should_Set_PageSize_To_MaxPageSize_When_Value_Exceeds_MaxPageSize()
        {
            // Arrange
            var specParam = new StudentClassSpecParam { PageIndex = 1, PageSize = BaseSpecParam.MaxPageSize + 10 };

            // Assert
            Assert.That(specParam.PageSize, Is.EqualTo(BaseSpecParam.MaxPageSize));
        }



        [Test]
        public void Should_Have_Error_When_SearchTerm_Exceeds_MaxLength()
        {
            // Arrange
            var longSearchTerm = new string('x', 256); // 256 characters
            var specParam = new StudentClassSpecParam { PageIndex = 1, PageSize = 10, SearchTerm = longSearchTerm };
            var query = new GetStudentClassesQuery(specParam, Guid.NewGuid());

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.StudentClassSpecParam.SearchTerm)
                .WithErrorMessage("Search term must not exceed 255 characters.");
        }

        [Test]
        public void Should_Not_Have_Error_When_Parameters_Are_Valid()
        {
            // Arrange
            var specParam = new StudentClassSpecParam
            {
                PageIndex = 1,
                PageSize = 10,
                SearchTerm = "Valid search term"
            };
            var query = new GetStudentClassesQuery(specParam, Guid.NewGuid());

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Should_Not_Have_Error_When_SearchTerm_Is_Null()
        {
            // Arrange
            var specParam = new StudentClassSpecParam
            {
                PageIndex = 1,
                PageSize = 10,
                SearchTerm = null
            };
            var query = new GetStudentClassesQuery(specParam, Guid.NewGuid());

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.StudentClassSpecParam.SearchTerm);
        }

        [Test]
        public void Should_Not_Have_Error_When_SearchTerm_Is_Empty()
        {
            // Arrange
            var specParam = new StudentClassSpecParam
            {
                PageIndex = 1,
                PageSize = 10,
                SearchTerm = string.Empty
            };
            var query = new GetStudentClassesQuery(specParam, Guid.NewGuid());

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.StudentClassSpecParam.SearchTerm);
        }

        [Test]
        public void Should_Not_Have_Error_When_PageSize_Equals_MaxPageSize()
        {
            // Arrange
            var specParam = new StudentClassSpecParam
            {
                PageIndex = 1,
                PageSize = BaseSpecParam.MaxPageSize
            };
            var query = new GetStudentClassesQuery(specParam, Guid.NewGuid());

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.StudentClassSpecParam.PageSize);
        }
    }
}