using Eduva.Application.Features.Folders.Queries;
using Eduva.Application.Features.Folders.Specifications;

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

            var folderSpecParam = new FolderSpecParam
            {
                ClassId = classId,
                UserId = userId
            };
            var query = new GetAllFoldersByClassIdQuery(folderSpecParam, userId);

            Assert.Multiple(() =>
            {
                Assert.That(query.FolderSpecParam.ClassId!.Value, Is.EqualTo(classId));
                Assert.That(query.UserId, Is.EqualTo(userId));
            });
        }
    }
}