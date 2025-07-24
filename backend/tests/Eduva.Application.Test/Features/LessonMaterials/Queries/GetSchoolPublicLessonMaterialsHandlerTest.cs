using Eduva.Application.Common.Models;
using Eduva.Application.Features.LessonMaterials.Queries.GetSchoolPublicLessonMaterials;
using Eduva.Application.Features.LessonMaterials.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Moq;

namespace Eduva.Application.Test.Features.LessonMaterials.Queries
{
    [TestFixture]
    public class GetSchoolPublicLessonMaterialsHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<ILessonMaterialRepository> _lessonMaterialRepoMock = null!;
        private GetSchoolPublicLessonMaterialsHandler _handler = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _lessonMaterialRepoMock = new Mock<ILessonMaterialRepository>();
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<ILessonMaterialRepository>())
                .Returns(_lessonMaterialRepoMock.Object);

            _handler = new GetSchoolPublicLessonMaterialsHandler(_unitOfWorkMock.Object);
        }

        [Test]
        public async Task Handle_Should_Return_Mapped_Pagination()
        {
            var param = new LessonMaterialSpecParam
            {
                EntityStatus = EntityStatus.Active,
                LessonStatus = LessonMaterialStatus.Approved,
                PageIndex = 1,
                PageSize = 10
            };
            var userId = Guid.NewGuid();

            var lessonMaterials = new Pagination<LessonMaterial>(
                pageIndex: 1,
                pageSize: 10,
                count: 2,
                data: new List<LessonMaterial>
                {
                    new LessonMaterial { Id = Guid.NewGuid(), Title = "Material 1" },
                    new LessonMaterial { Id = Guid.NewGuid(), Title = "Material 2" }
                });

            _lessonMaterialRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<PublicLessonMaterialSpecification>()))
                .ReturnsAsync(lessonMaterials);


            var query = new GetSchoolPublicLessonMaterialsQuery(param, userId);
            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(result.Data.First().Title, Is.EqualTo("Material 1"));
                Assert.That(result.Data.ElementAt(1).Title, Is.EqualTo("Material 2"));
            });
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

            var lessonMaterials = new Pagination<LessonMaterial>(
                pageIndex: 1,
                pageSize: 10,
                count: 0,
                data: new List<LessonMaterial>());

            _lessonMaterialRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<PublicLessonMaterialSpecification>()))
                .ReturnsAsync(lessonMaterials);


            var query = new GetSchoolPublicLessonMaterialsQuery(param, userId);
            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
            Assert.That(result.Data, Is.Empty);
        }
    }
}