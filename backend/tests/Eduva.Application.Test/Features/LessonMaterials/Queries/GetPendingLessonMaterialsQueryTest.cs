using Eduva.Application.Features.LessonMaterials.Queries.GetPendingLessonMaterials;
using Eduva.Application.Features.LessonMaterials.Specifications;
using Eduva.Domain.Enums;

namespace Eduva.Application.Test.Features.LessonMaterials.Queries
{
    [TestFixture]
    public class GetPendingLessonMaterialsQueryTest
    {
        [Test]
        public void Constructor_Should_Set_Properties_Correctly()
        {
            var param = new LessonMaterialSpecParam
            {
                EntityStatus = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Pending,
                PageIndex = 1,
                PageSize = 10
            };
            var userId = Guid.NewGuid();
            var userRoles = new List<string> { "Teacher", "SchoolAdmin" };

            var query = new GetPendingLessonMaterialsQuery(param, userId, userRoles);

            Assert.Multiple(() =>
            {
                Assert.That(query.LessonMaterialSpecParam, Is.EqualTo(param));
                Assert.That(query.UserId, Is.EqualTo(userId));
                Assert.That(query.UserRoles, Is.EqualTo(userRoles));
            });
        }
    }
}