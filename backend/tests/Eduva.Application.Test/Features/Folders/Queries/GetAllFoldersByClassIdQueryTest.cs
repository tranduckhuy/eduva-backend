using Eduva.Application.Features.Folders.Queries;

namespace Eduva.Application.Test.Features.Folders.Queries
{
    [TestFixture]
    public class GetAllFoldersByClassIdQueryTest
    {
        [Test]
        public void Constructor_Should_Set_Properties_Correctly()
        {
            var classId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var query = new GetAllFoldersByClassIdQuery(classId, userId);

            Assert.Multiple(() =>
            {
                Assert.That(query.ClassId, Is.EqualTo(classId));
                Assert.That(query.UserId, Is.EqualTo(userId));
            });
        }
    }
}