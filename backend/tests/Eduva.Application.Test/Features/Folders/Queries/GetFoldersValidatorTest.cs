using Eduva.Application.Common.Specifications;
using Eduva.Application.Features.Folders.Queries;
using Eduva.Application.Features.Folders.Specifications;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.Folders.Queries
{
    [TestFixture]
    public class GetFoldersValidatorTest
    {
        #region Setup
        private GetFoldersValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new GetFoldersValidator();
        }
        #endregion

        #region Tests
        [Test]
        public void Should_Have_Error_When_PageIndex_Less_Than_1()
        {
            var model = new GetFoldersQuery(new FolderSpecParam { PageIndex = 0, PageSize = 10 });
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.FolderSpecParam.PageIndex);
        }

        [Test]
        public void Should_Have_Error_When_PageIndex_Negative()
        {
            var model = new GetFoldersQuery(new FolderSpecParam { PageIndex = -5, PageSize = 10 });
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.FolderSpecParam.PageIndex);
        }

        [Test]
        public void Should_Have_Error_When_PageSize_Less_Than_1()
        {
            var model = new GetFoldersQuery(new FolderSpecParam { PageIndex = 1, PageSize = 0 });
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.FolderSpecParam.PageSize);
        }

        [Test]
        public void Should_Have_Error_When_PageSize_Negative()
        {
            var model = new GetFoldersQuery(new FolderSpecParam { PageIndex = 1, PageSize = -10 });
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.FolderSpecParam.PageSize);
        }

        [Test]
        public void Should_Not_Have_Error_When_Valid()
        {
            var model = new GetFoldersQuery(new FolderSpecParam { PageIndex = 1, PageSize = 10 });
            var result = _validator.TestValidate(model);
            result.ShouldNotHaveValidationErrorFor(x => x.FolderSpecParam.PageIndex);
            result.ShouldNotHaveValidationErrorFor(x => x.FolderSpecParam.PageSize);
        }

        [Test]
        public void Should_Cap_PageSize_When_Exceeds_Max()
        {
            var model = new FolderSpecParam { PageIndex = 1, PageSize = BaseSpecParam.MaxPageSize + 10 };
            Assert.That(model.PageSize, Is.EqualTo(BaseSpecParam.MaxPageSize));
        }

        [Test]
        public void Should_Have_Multiple_Errors_When_Both_Invalid()
        {
            var model = new GetFoldersQuery(new FolderSpecParam { PageIndex = 0, PageSize = 0 });
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.FolderSpecParam.PageIndex);
            result.ShouldHaveValidationErrorFor(x => x.FolderSpecParam.PageSize);
        }
        #endregion
    }
}
