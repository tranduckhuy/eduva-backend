using Eduva.Application.Features.LessonMaterials.Queries.GetSchoolPublicLessonMaterials;
using Eduva.Application.Features.LessonMaterials.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.LessonMaterials.Queries
{
    [TestFixture]
    public class GetSchoolPublicLessonMaterialsValidatorTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private GetSchoolPublicLessonMaterialsValidator _validator = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

            _validator = new GetSchoolPublicLessonMaterialsValidator(_unitOfWorkMock.Object, _userManagerMock.Object);
        }

        [Test]
        public void Should_Have_Error_When_SearchTerm_Too_Long()
        {
            var query = new GetSchoolPublicLessonMaterialsQuery(
                new LessonMaterialSpecParam
                {
                    SearchTerm = new string('a', 256),
                    ContentType = ContentType.Video
                },
                Guid.NewGuid() 
            );
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor("LessonMaterialSpecParam.SearchTerm");
        }

        [Test]
        public void Should_Have_Error_When_ContentType_Is_Invalid()
        {
            var query = new GetSchoolPublicLessonMaterialsQuery(
                new LessonMaterialSpecParam
                {
                    SearchTerm = "abc",
                    ContentType = (ContentType)999
                },
                Guid.NewGuid()
            );
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor("LessonMaterialSpecParam.ContentType");
        }

        [Test]
        public void Should_Not_Have_Error_For_Valid_Query()
        {
            var query = new GetSchoolPublicLessonMaterialsQuery(
                new LessonMaterialSpecParam
                {
                    SearchTerm = "abc",
                    ContentType = ContentType.Video
                },
                Guid.NewGuid()
            );
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}