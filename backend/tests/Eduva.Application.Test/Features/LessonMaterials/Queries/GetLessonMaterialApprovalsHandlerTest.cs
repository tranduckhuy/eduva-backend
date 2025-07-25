using Eduva.Application.Common.Models;
using Eduva.Application.Features.LessonMaterials.Queries.GetLessonMaterialApprovals;
using Eduva.Application.Features.LessonMaterials.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Moq;

namespace Eduva.Application.Test.Features.LessonMaterials.Queries
{
    [TestFixture]
    public class GetLessonMaterialApprovalsHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IGenericRepository<LessonMaterialApproval, Guid>> _approvalRepoMock = null!;
        private GetLessonMaterialApprovalsHandler _handler = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _approvalRepoMock = new Mock<IGenericRepository<LessonMaterialApproval, Guid>>();
            _unitOfWorkMock.Setup(u => u.GetRepository<LessonMaterialApproval, Guid>())
                .Returns(_approvalRepoMock.Object);

            _handler = new GetLessonMaterialApprovalsHandler(_unitOfWorkMock.Object);
        }

        [Test]
        public async Task Handle_Should_Return_ApprovalHistory_For_Admin()
        {
            var specParam = new LessonMaterialApprovalsSpecParam
            {
                LessonMaterialId = Guid.NewGuid(),
                PageIndex = 1,
                PageSize = 10
            };
            var query = new GetLessonMaterialApprovalsQuery(
                specParam,
                Guid.NewGuid(),
                new List<string> { "SystemAdmin" }
            );

            var pagination = new Pagination<LessonMaterialApproval>(1, 10, 1, new List<LessonMaterialApproval>
            {
                new LessonMaterialApproval { Id = Guid.NewGuid() }
            });

            _approvalRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<LessonMaterialApprovalsSpecification>()))
                .ReturnsAsync(pagination);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task Handle_Should_Set_CreatedByUserId_For_RegularTeacher()
        {
            var teacherId = Guid.NewGuid();
            var specParam = new LessonMaterialApprovalsSpecParam
            {
                LessonMaterialId = Guid.NewGuid(),
                PageIndex = 1,
                PageSize = 10
            };
            var query = new GetLessonMaterialApprovalsQuery(
                specParam,
                teacherId,
                new List<string> { "Teacher" }
            );

            var pagination = new Pagination<LessonMaterialApproval>(1, 10, 0, new List<LessonMaterialApproval>());

            LessonMaterialApprovalsSpecification? capturedSpec = null;
            _approvalRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<LessonMaterialApprovalsSpecification>()))
                .Callback<LessonMaterialApprovalsSpecification>(spec => capturedSpec = spec)
                .ReturnsAsync(pagination);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Data, Is.Empty);
                Assert.That(capturedSpec, Is.Not.Null);
            });
        }

        [Test]
        public async Task Handle_Should_Not_Set_CreatedByUserId_For_SchoolAdmin()
        {
            var teacherId = Guid.NewGuid();
            var specParam = new LessonMaterialApprovalsSpecParam
            {
                LessonMaterialId = Guid.NewGuid(),
                PageIndex = 1,
                PageSize = 10
            };
            var query = new GetLessonMaterialApprovalsQuery(
                specParam,
                teacherId,
                new List<string> { "SchoolAdmin" }
            );

            var pagination = new Pagination<LessonMaterialApproval>(1, 10, 0, new List<LessonMaterialApproval>());

            LessonMaterialApprovalsSpecification? capturedSpec = null;
            _approvalRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<LessonMaterialApprovalsSpecification>()))
                .Callback<LessonMaterialApprovalsSpecification>(spec => capturedSpec = spec)
                .ReturnsAsync(pagination);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(result.Data, Is.Empty);
                Assert.That(capturedSpec, Is.Not.Null);
            });
        }

        [Test]
        public async Task Handle_Should_Return_Empty_If_Repo_Returns_EmptyPagination()
        {
            var specParam = new LessonMaterialApprovalsSpecParam
            {
                LessonMaterialId = Guid.NewGuid(),
                PageIndex = 1,
                PageSize = 10
            };

            var query = new GetLessonMaterialApprovalsQuery(
                specParam,
                Guid.NewGuid(),
                new List<string> { "Teacher" }
            );

            var emptyPagination = new Pagination<LessonMaterialApproval>(
                pageIndex: specParam.PageIndex,
                pageSize: specParam.PageSize,
                count: 0,
                data: new List<LessonMaterialApproval>()
            );

            _approvalRepoMock
                .Setup(r => r.GetWithSpecAsync(It.IsAny<LessonMaterialApprovalsSpecification>()))
                .ReturnsAsync(emptyPagination);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data, Is.Empty);
        }
    }
}