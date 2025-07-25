using Eduva.Application.Features.LessonMaterials.Queries.GetSchoolPublicLessonMaterials;
using Eduva.Application.Features.LessonMaterials.Specifications;
using Eduva.Domain.Enums;

namespace Eduva.Application.Test.Features.LessonMaterials.Queries
{
    [TestFixture]
    public class GetSchoolPublicLessonMaterialsQueryTest
    {
        [Test]
        public void Constructor_Should_Set_Properties_Correctly()
        {
            var param = new LessonMaterialSpecParam
            {
                EntityStatus = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                PageIndex = 1,
                PageSize = 10
            };
            var userId = Guid.NewGuid();

            var query = new GetSchoolPublicLessonMaterialsQuery(param, userId);

            Assert.Multiple(() =>
            {
                Assert.That(query.LessonMaterialSpecParam, Is.EqualTo(param));
                Assert.That(query.UserId, Is.EqualTo(userId));
            });
        }
    }
}