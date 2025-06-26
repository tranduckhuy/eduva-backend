using Eduva.Application.Exceptions.School;
using Eduva.Application.Features.Schools.Queries;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Moq;

namespace Eduva.Application.Test.Features.Schools.Queries;

[TestFixture]
public class GetSchoolByIdQueryHandlerTests
{
    private Mock<ISchoolRepository> _schoolRepoMock = default!;
    private Mock<IUserRepository> _userRepoMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private GetSchoolByIdQueryHandler _handler = default!;

    #region Setup

    [SetUp]
    public void SetUp()
    {
        _schoolRepoMock = new Mock<ISchoolRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(u => u.GetCustomRepository<ISchoolRepository>())
            .Returns(_schoolRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetCustomRepository<IUserRepository>())
            .Returns(_userRepoMock.Object);

        _handler = new GetSchoolByIdQueryHandler(_unitOfWorkMock.Object);
    }

    #endregion

    #region Tests

    [Test]
    public async Task Handle_ShouldReturnSchoolDetail_WhenSchoolAndAdminExist()
    {
        // Arrange
        var schoolId = 1;
        var school = new School
        {
            Id = schoolId,
            Name = "Test School",
            ContactEmail = "school@test.com",
            ContactPhone = "123456",
            Address = "123 Street",
            WebsiteUrl = "https://school.com",
            Status = EntityStatus.Active
        };
        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = "Admin Name",
            Email = "admin@test.com"
        };

        _schoolRepoMock.Setup(r => r.GetByIdAsync(schoolId)).ReturnsAsync(school);
        _userRepoMock.Setup(r => r.GetSchoolAdminBySchoolIdAsync(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);

        var query = new GetSchoolByIdQuery(schoolId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.Id, Is.EqualTo(schoolId));
            Assert.That(result.SchoolAdminId, Is.EqualTo(admin.Id));
            Assert.That(result.SchoolAdminFullName, Is.EqualTo(admin.FullName));
            Assert.That(result.SchoolAdminEmail, Is.EqualTo(admin.Email));
        });
    }

    [Test]
    public async Task Handle_ShouldReturnSchoolDetail_WithNullAdmin_WhenAdminNotFound()
    {
        var schoolId = 2;
        var school = new School
        {
            Id = schoolId,
            Name = "School Without Admin",
            ContactEmail = "test@noadmin.com",
            ContactPhone = "000000",
            Address = "No Admin Street",
            WebsiteUrl = null,
            Status = EntityStatus.Archived
        };

        _schoolRepoMock.Setup(r => r.GetByIdAsync(schoolId)).ReturnsAsync(school);
        _userRepoMock.Setup(r => r.GetSchoolAdminBySchoolIdAsync(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        var query = new GetSchoolByIdQuery(schoolId);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.SchoolAdminId, Is.Null);
            Assert.That(result.SchoolAdminFullName, Is.Null);
            Assert.That(result.SchoolAdminEmail, Is.Null);
        });
    }

    [Test]
    public void Handle_ShouldThrow_WhenSchoolNotFound()
    {
        _schoolRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((School?)null);
        var query = new GetSchoolByIdQuery(100);

        Assert.ThrowsAsync<SchoolNotFoundException>(() => _handler.Handle(query, CancellationToken.None));
    }

    #endregion

}