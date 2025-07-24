using Eduva.Application.Features.Folders.Queries;
using Eduva.Application.Features.Folders.Specifications;
using Eduva.Application.Common.Specifications;
using Eduva.Domain.Enums;

namespace Eduva.Application.Test.Features.Folders.Queries
{
    [TestFixture]
    public class GetAllUserFoldersQueryTest
    {
        [Test]
        public void Constructor_Should_Set_Properties_Correctly()
        {
            var param = new FolderSpecParam
            {
                Name = "Test Folder",
                UserId = Guid.NewGuid(),
                ClassId = Guid.NewGuid(),
                OwnerType = OwnerType.Personal,
                Status = EntityStatus.Active
            };

            var query = new GetAllUserFoldersQuery(param);

            Assert.That(query.FolderSpecParam, Is.EqualTo(param));
        }
    }
}