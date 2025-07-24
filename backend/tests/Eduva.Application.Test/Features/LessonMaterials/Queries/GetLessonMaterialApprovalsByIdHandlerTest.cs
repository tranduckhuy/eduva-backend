using AutoMapper;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.LessonMaterials.Queries.GetLessonMaterialApprovalsById;
using Eduva.Application.Features.LessonMaterials.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.LessonMaterials.Queries
{
    [TestFixture]
    public class GetLessonMaterialApprovalsByIdHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<ILessonMaterialRepository> _materialRepoMock = null!;
        private Mock<IUserRepository> _userRepoMock = null!;
        private Mock<IGenericRepository<LessonMaterialApproval, Guid>> _approvalRepoMock = null!;
        private Mock<IMapper> _mapperMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private GetLessonMaterialApprovalsByIdHandler _handler = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _materialRepoMock = new Mock<ILessonMaterialRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _approvalRepoMock = new Mock<IGenericRepository<LessonMaterialApproval, Guid>>();
            _mapperMock = new Mock<IMapper>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<ILessonMaterialRepository>())
                .Returns(_materialRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IUserRepository>())
                .Returns(_userRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<LessonMaterialApproval, Guid>())
                .Returns(_approvalRepoMock.Object);

            _handler = new GetLessonMaterialApprovalsByIdHandler(
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _userManagerMock.Object
            );
        }

        [Test]
        public void Handle_Should_Throw_When_LessonMaterial_Not_Found()
        {
            var query = new GetLessonMaterialApprovalsByIdQuery(Guid.NewGuid(), Guid.NewGuid());

            _materialRepoMock.Setup(r => r.GetByIdAsync(query.LessonMaterialId)).ReturnsAsync((LessonMaterial?)null);

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(query, CancellationToken.None));
        }

        [Test]
        public void Handle_Should_Throw_When_User_Not_Found()
        {
            var lessonMaterialId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var query = new GetLessonMaterialApprovalsByIdQuery(lessonMaterialId, userId);

            _materialRepoMock.Setup(r => r.GetByIdAsync(lessonMaterialId)).ReturnsAsync(new LessonMaterial { Id = lessonMaterialId });
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null);

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(query, CancellationToken.None));
        }

        [Test]
        public void Handle_Should_Throw_Forbidden_For_SchoolAdmin_If_SchoolId_Not_Match()
        {
            var lessonMaterialId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var query = new GetLessonMaterialApprovalsByIdQuery(lessonMaterialId, userId);

            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, SchoolId = 2 };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };

            _materialRepoMock.Setup(r => r.GetByIdAsync(lessonMaterialId)).ReturnsAsync(lessonMaterial);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.SchoolAdmin) });

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(query, CancellationToken.None));
        }

        [Test]
        public void Handle_Should_Throw_Forbidden_For_ContentModerator_If_SchoolId_Not_Match()
        {
            var lessonMaterialId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var query = new GetLessonMaterialApprovalsByIdQuery(lessonMaterialId, userId);

            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, SchoolId = 2 };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };

            _materialRepoMock.Setup(r => r.GetByIdAsync(lessonMaterialId)).ReturnsAsync(lessonMaterial);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.ContentModerator) });

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(query, CancellationToken.None));
        }

        [Test]
        public void Handle_Should_Throw_Forbidden_For_Teacher_If_Not_Owner()
        {
            var lessonMaterialId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var query = new GetLessonMaterialApprovalsByIdQuery(lessonMaterialId, userId);

            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, CreatedByUserId = Guid.NewGuid() };
            var user = new ApplicationUser { Id = userId };

            _materialRepoMock.Setup(r => r.GetByIdAsync(lessonMaterialId)).ReturnsAsync(lessonMaterial);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.Teacher) });

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(query, CancellationToken.None));
        }

        [Test]
        public void Handle_Should_Throw_Forbidden_For_Other_Roles()
        {
            var lessonMaterialId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var query = new GetLessonMaterialApprovalsByIdQuery(lessonMaterialId, userId);

            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, CreatedByUserId = userId };
            var user = new ApplicationUser { Id = userId };

            _materialRepoMock.Setup(r => r.GetByIdAsync(lessonMaterialId)).ReturnsAsync(lessonMaterial);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Student" });

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(query, CancellationToken.None));
        }

        [Test]
        public async Task Handle_Should_Return_Approvals_For_SystemAdmin()
        {
            var lessonMaterialId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var query = new GetLessonMaterialApprovalsByIdQuery(lessonMaterialId, userId);

            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, CreatedByUserId = userId };
            var user = new ApplicationUser { Id = userId };

            _materialRepoMock.Setup(r => r.GetByIdAsync(lessonMaterialId)).ReturnsAsync(lessonMaterial);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.SystemAdmin) });

            var approval1 = new LessonMaterialApproval { Id = Guid.NewGuid(), LessonMaterialId = lessonMaterialId, CreatedAt = DateTimeOffset.UtcNow };
            var approval2 = new LessonMaterialApproval { Id = Guid.NewGuid(), LessonMaterialId = lessonMaterialId, CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1) };

            // Chuỗi truy vấn approval theo CreatedAt giảm dần
            _approvalRepoMock.SetupSequence(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<LessonMaterialApproval, bool>>>(),
                It.IsAny<Func<IQueryable<LessonMaterialApproval>, IQueryable<LessonMaterialApproval>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(approval1)
                .ReturnsAsync(approval2)
                .ReturnsAsync((LessonMaterialApproval?)null);

            _mapperMock.Setup(m => m.Map<List<LessonMaterialApprovalResponse>>(It.IsAny<List<LessonMaterialApproval>>()))
                .Returns((List<LessonMaterialApproval> approvals) =>
                    approvals.ConvertAll(a => new LessonMaterialApprovalResponse { Id = a.Id }));

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].Id, Is.EqualTo(approval1.Id));
            Assert.That(result[1].Id, Is.EqualTo(approval2.Id));
        }

        [Test]
        public async Task Handle_Should_Return_Approvals_For_SchoolAdmin_If_SchoolId_Match()
        {
            var lessonMaterialId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var query = new GetLessonMaterialApprovalsByIdQuery(lessonMaterialId, userId);

            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, SchoolId = 1, CreatedByUserId = userId };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };

            _materialRepoMock.Setup(r => r.GetByIdAsync(lessonMaterialId)).ReturnsAsync(lessonMaterial);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.SchoolAdmin) });

            var approval = new LessonMaterialApproval { Id = Guid.NewGuid(), LessonMaterialId = lessonMaterialId, CreatedAt = DateTimeOffset.UtcNow };

            _approvalRepoMock.SetupSequence(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<LessonMaterialApproval, bool>>>(),
                It.IsAny<Func<IQueryable<LessonMaterialApproval>, IQueryable<LessonMaterialApproval>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(approval)
                .ReturnsAsync((LessonMaterialApproval?)null);

            _mapperMock.Setup(m => m.Map<List<LessonMaterialApprovalResponse>>(It.IsAny<List<LessonMaterialApproval>>()))
                .Returns((List<LessonMaterialApproval> approvals) =>
                    approvals.ConvertAll(a => new LessonMaterialApprovalResponse { Id = a.Id }));

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Id, Is.EqualTo(approval.Id));
        }

        [Test]
        public async Task Handle_Should_Return_Approvals_For_Teacher_If_Owner()
        {
            var lessonMaterialId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var query = new GetLessonMaterialApprovalsByIdQuery(lessonMaterialId, userId);

            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, CreatedByUserId = userId };
            var user = new ApplicationUser { Id = userId };

            _materialRepoMock.Setup(r => r.GetByIdAsync(lessonMaterialId)).ReturnsAsync(lessonMaterial);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.Teacher) });

            var approval = new LessonMaterialApproval { Id = Guid.NewGuid(), LessonMaterialId = lessonMaterialId, CreatedAt = DateTimeOffset.UtcNow };

            _approvalRepoMock.SetupSequence(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<LessonMaterialApproval, bool>>>(),
                It.IsAny<Func<IQueryable<LessonMaterialApproval>, IQueryable<LessonMaterialApproval>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(approval)
                .ReturnsAsync((LessonMaterialApproval?)null);

            _mapperMock.Setup(m => m.Map<List<LessonMaterialApprovalResponse>>(It.IsAny<List<LessonMaterialApproval>>()))
                .Returns((List<LessonMaterialApproval> approvals) =>
                    approvals.ConvertAll(a => new LessonMaterialApprovalResponse { Id = a.Id }));

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Id, Is.EqualTo(approval.Id));
        }

        [Test]
        public async Task Handle_Should_Return_Empty_If_No_Approvals()
        {
            var lessonMaterialId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var query = new GetLessonMaterialApprovalsByIdQuery(lessonMaterialId, userId);

            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, CreatedByUserId = userId };
            var user = new ApplicationUser { Id = userId };

            _materialRepoMock.Setup(r => r.GetByIdAsync(lessonMaterialId)).ReturnsAsync(lessonMaterial);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.SystemAdmin) });

            _approvalRepoMock.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<LessonMaterialApproval, bool>>>(),
                It.IsAny<Func<IQueryable<LessonMaterialApproval>, IQueryable<LessonMaterialApproval>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((LessonMaterialApproval?)null);

            _mapperMock.Setup(m => m.Map<List<LessonMaterialApprovalResponse>>(It.IsAny<List<LessonMaterialApproval>>()))
                .Returns(new List<LessonMaterialApprovalResponse>());

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.Empty);
        }
    }
}