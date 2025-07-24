using Eduva.Application.Common.Models;
using Eduva.Application.Features.LessonMaterials.Queries.GetPendingLessonMaterials;
using Eduva.Application.Features.LessonMaterials.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Moq;

namespace Eduva.Application.Test.Features.LessonMaterials.Queries
{
    [TestFixture]
    public class GetPendingLessonMaterialsHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<ILessonMaterialRepository> _lessonMaterialRepoMock = null!;
        private GetPendingLessonMaterialsHandler _handler = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _lessonMaterialRepoMock = new Mock<ILessonMaterialRepository>();
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<ILessonMaterialRepository>())
                .Returns(_lessonMaterialRepoMock.Object);

            _handler = new GetPendingLessonMaterialsHandler(_unitOfWorkMock.Object);
        }

        [Test]
        public async Task Handle_Should_Return_PendingMaterials_For_Teacher()
        {
            var param = new LessonMaterialSpecParam
            {
                EntityStatus = EntityStatus.Active,
                PageIndex = 1,
                PageSize = 10
            };
            var userId = Guid.NewGuid();
            var userRoles = new List<string> { "Teacher" };

            var pendingMaterials = new Pagination<LessonMaterial>(
                pageIndex: 1,
                pageSize: 10,
                count: 1,
                data: new List<LessonMaterial>
                {
                    new LessonMaterial { Id = Guid.NewGuid(), Title = "Pending Material", LessonStatus = LessonMaterialStatus.Pending, CreatedByUserId = userId }
                });

            _lessonMaterialRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<PendingLessonMaterialSpecification>()))
                .ReturnsAsync(pendingMaterials);

            var query = new GetPendingLessonMaterialsQuery(param, userId, userRoles);
            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.Data.First().Title, Is.EqualTo("Pending Material"));
        }

        [Test]
        public async Task Handle_Should_Return_PendingMaterials_For_Admin()
        {
            var param = new LessonMaterialSpecParam
            {
                EntityStatus = EntityStatus.Active,
                PageIndex = 1,
                PageSize = 10
            };
            var userId = Guid.NewGuid();
            var userRoles = new List<string> { "SchoolAdmin" };

            var pendingMaterials = new Pagination<LessonMaterial>(
                pageIndex: 1,
                pageSize: 10,
                count: 1,
                data: new List<LessonMaterial>
                {
                    new LessonMaterial { Id = Guid.NewGuid(), Title = "Admin Pending Material", LessonStatus = LessonMaterialStatus.Pending }
                });

            _lessonMaterialRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<PendingLessonMaterialSpecification>()))
                .ReturnsAsync(pendingMaterials);


            var query = new GetPendingLessonMaterialsQuery(param, userId, userRoles);
            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.Data.First().Title, Is.EqualTo("Admin Pending Material"));
        }

        [Test]
        public async Task Handle_Should_Return_Empty_If_Repo_Returns_Empty()
        {
            var param = new LessonMaterialSpecParam
            {
                PageIndex = 1,
                PageSize = 10
            };
            var userId = Guid.NewGuid();
            var userRoles = new List<string> { "Teacher" };

            var emptyMaterials = new Pagination<LessonMaterial>(
                pageIndex: 1,
                pageSize: 10,
                count: 0,
                data: new List<LessonMaterial>());

            _lessonMaterialRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<PendingLessonMaterialSpecification>()))
                .ReturnsAsync(emptyMaterials);


            var query = new GetPendingLessonMaterialsQuery(param, userId, userRoles);
            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
            Assert.That(result.Data, Is.Empty);
        }
    }
}