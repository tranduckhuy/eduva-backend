using Eduva.Application.Features.LessonMaterials.DTOs;
using Eduva.Application.Features.LessonMaterials.Queries.GetLessonMaterialsByFolder;

namespace Eduva.Application.Test.Features.LessonMaterials.Queries
{
    [TestFixture]
    public class GetLessonMaterialsByFolderQueryTest
    {
        [Test]
        public void Constructor_Should_Set_Properties_Correctly()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var schoolId = 123;
            var userRoles = new List<string> { "Teacher", "SchoolAdmin" };
            var filterOptions = new LessonMaterialFilterOptions
            {
                Status = Eduva.Domain.Enums.EntityStatus.Active,
                LessonStatus = Eduva.Domain.Enums.LessonMaterialStatus.Approved
            };

            var query = new GetLessonMaterialsByFolderQuery(
                folderId,
                userId,
                schoolId,
                userRoles,
                filterOptions
            );

            Assert.Multiple(() =>
            {
                Assert.That(query.FolderId, Is.EqualTo(folderId));
                Assert.That(query.UserId, Is.EqualTo(userId));
                Assert.That(query.SchoolId, Is.EqualTo(schoolId));
                Assert.That(query.UserRoles, Is.EqualTo(userRoles));
                Assert.That(query.FilterOptions, Is.EqualTo(filterOptions));
            });
        }

        [Test]
        public void Constructor_Should_Set_FilterOptions_Null_By_Default()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var schoolId = 123;
            var userRoles = new List<string> { "Teacher" };

            var query = new GetLessonMaterialsByFolderQuery(
                folderId,
                userId,
                schoolId,
                userRoles
            );

            Assert.That(query.FilterOptions, Is.Null);
        }
    }
}