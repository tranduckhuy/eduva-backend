using Eduva.Application.Features.Folders.Queries;

namespace Eduva.Application.Test.Features.Folders.Queries
{
    [TestFixture]
    public class GetFolderByIdQueryTest
    {
        [Test]
        public void Constructor_Should_Set_Properties_Correctly()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var query = new GetFolderByIdQuery(folderId, userId);

            Assert.Multiple(() =>
            {
                Assert.That(query.Id, Is.EqualTo(folderId));
                Assert.That(query.UserId, Is.EqualTo(userId));
            });
        }
    }
}