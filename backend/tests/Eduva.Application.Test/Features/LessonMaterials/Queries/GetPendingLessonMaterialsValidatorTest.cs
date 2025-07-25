using Eduva.Application.Features.LessonMaterials.Queries.GetPendingLessonMaterials;
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
    public class GetPendingLessonMaterialsValidatorTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private GetPendingLessonMaterialsValidator _validator = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

            _validator = new GetPendingLessonMaterialsValidator(_unitOfWorkMock.Object, _userManagerMock.Object);
        }

        private static GetPendingLessonMaterialsQuery CreateValidQuery(Guid? userId = null, int? schoolId = null, List<string>? roles = null)
        {
            return new GetPendingLessonMaterialsQuery(
                new LessonMaterialSpecParam
                {
                    SchoolId = schoolId ?? 1,
                    SearchTerm = "abc",
                    ContentType = ContentType.Video
                },
                userId ?? Guid.NewGuid(),
                roles ?? new List<string> { nameof(Role.SchoolAdmin) }
            );
        }

        [Test]
        public async Task Should_Have_Error_When_UserId_Is_Empty()
        {
            var query = CreateValidQuery(Guid.Empty);
            var result = await _validator.TestValidateAsync(query);
            result.ShouldHaveValidationErrorFor(q => q.UserId);
        }

        [Test]
        public async Task Should_Have_Error_When_SchoolId_Is_Invalid()
        {
            var query = CreateValidQuery(schoolId: 0);
            var result = await _validator.TestValidateAsync(query);
            result.ShouldHaveValidationErrorFor("LessonMaterialSpecParam.SchoolId");
        }

        [Test]
        public async Task Should_Have_Error_When_UserRoles_Is_Empty()
        {
            var query = CreateValidQuery(roles: new List<string>());
            var result = await _validator.TestValidateAsync(query);
            result.ShouldHaveValidationErrorFor(q => q.UserRoles);
        }

        [Test]
        public async Task Should_Have_Error_When_UserRoles_Is_Invalid()
        {
            var query = CreateValidQuery(roles: new List<string> { "Student" });
            var result = await _validator.TestValidateAsync(query);
            result.ShouldHaveValidationErrorFor(q => q.UserRoles);
        }

        [Test]
        public async Task Should_Have_Error_When_SearchTerm_Too_Long()
        {
            var query = CreateValidQuery();
            query.LessonMaterialSpecParam.SearchTerm = new string('a', 256);
            var result = await _validator.TestValidateAsync(query);
            result.ShouldHaveValidationErrorFor("LessonMaterialSpecParam.SearchTerm");
        }

        [Test]
        public async Task Should_Have_Error_When_ContentType_Is_Invalid()
        {
            var query = CreateValidQuery();
            query.LessonMaterialSpecParam.ContentType = (ContentType)999;
            var result = await _validator.TestValidateAsync(query);
            result.ShouldHaveValidationErrorFor("LessonMaterialSpecParam.ContentType");
        }

        [Test]
        public async Task Should_Have_Error_When_User_Not_Found()
        {
            var query = CreateValidQuery();
            _userManagerMock.Setup(m => m.FindByIdAsync(query.UserId.ToString())).ReturnsAsync((ApplicationUser?)null);

            var result = await _validator.TestValidateAsync(query);
            result.ShouldHaveValidationErrorFor(q => q);
        }

        [Test]
        public async Task Should_Have_Error_When_User_SchoolId_Not_Match()
        {
            var query = CreateValidQuery();
            var user = new ApplicationUser { Id = query.UserId, SchoolId = 999 };
            _userManagerMock.Setup(m => m.FindByIdAsync(query.UserId.ToString())).ReturnsAsync(user);

            var result = await _validator.TestValidateAsync(query);
            result.ShouldHaveValidationErrorFor(q => q);
        }

        [Test]
        public async Task Should_Not_Have_Error_For_SchoolAdmin()
        {
            var query = CreateValidQuery();
            var user = new ApplicationUser { Id = query.UserId, SchoolId = query.LessonMaterialSpecParam.SchoolId };
            _userManagerMock.Setup(m => m.FindByIdAsync(query.UserId.ToString())).ReturnsAsync(user);

            var result = await _validator.TestValidateAsync(query);
            result.ShouldNotHaveValidationErrorFor(q => q);
        }

        [Test]
        public async Task Should_Not_Have_Error_For_ContentModerator()
        {
            var query = CreateValidQuery(roles: new List<string> { nameof(Role.ContentModerator) });
            var user = new ApplicationUser { Id = query.UserId, SchoolId = query.LessonMaterialSpecParam.SchoolId };
            _userManagerMock.Setup(m => m.FindByIdAsync(query.UserId.ToString())).ReturnsAsync(user);

            var result = await _validator.TestValidateAsync(query);
            result.ShouldNotHaveValidationErrorFor(q => q);
        }

        [Test]
        public async Task Should_Not_Have_Error_For_Teacher()
        {
            var query = CreateValidQuery(roles: new List<string> { nameof(Role.Teacher) });
            var user = new ApplicationUser { Id = query.UserId, SchoolId = query.LessonMaterialSpecParam.SchoolId };
            _userManagerMock.Setup(m => m.FindByIdAsync(query.UserId.ToString())).ReturnsAsync(user);

            var result = await _validator.TestValidateAsync(query);
            result.ShouldNotHaveValidationErrorFor(q => q);
        }
    }
}