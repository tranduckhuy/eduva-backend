using Eduva.Application.Features.LessonMaterials.Queries.GetLessonMaterialApprovals;
using Eduva.Application.Features.LessonMaterials.Specifications;
using Eduva.Domain.Enums;

namespace Eduva.Application.Test.Features.LessonMaterials.Queries
{
    [TestFixture]
    public class GetLessonMaterialApprovalsQueryTest
    {
        [Test]
        public void Constructor_Should_Set_Properties_Correctly()
        {
            var specParam = new LessonMaterialApprovalsSpecParam
            {
                LessonMaterialId = Guid.NewGuid(),
                StatusChangeTo = LessonMaterialStatus.Approved,
                PageIndex = 1,
                PageSize = 10
            };
            var userId = Guid.NewGuid();
            var userRoles = new List<string> { "Teacher", "SchoolAdmin" };

            var query = new GetLessonMaterialApprovalsQuery(specParam, userId, userRoles);

            Assert.Multiple(() =>
            {
                Assert.That(query.SpecParam, Is.EqualTo(specParam));
                Assert.That(query.UserId, Is.EqualTo(userId));
                Assert.That(query.UserRoles, Is.EqualTo(userRoles));
            });
        }
    }
}