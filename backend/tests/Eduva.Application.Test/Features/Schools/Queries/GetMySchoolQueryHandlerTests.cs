using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Exceptions.School;
using Eduva.Application.Features.Schools.Queries;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Moq;

namespace Eduva.Application.Test.Features.Schools.Queries;

[TestFixture]
public class GetMySchoolQueryHandlerTests
{
    private Mock<IUserRepository> _userRepoMock = default!;
    private Mock<ISchoolRepository> _schoolRepoMock = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private GetMySchoolQueryHandler _handler = default!;

    #region GetMySchoolQueryHandlerTests Setup

    [SetUp]
    public void Setup()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _schoolRepoMock = new Mock<ISchoolRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.GetCustomRepository<IUserRepository>())
            .Returns(_userRepoMock.Object);
        _unitOfWorkMock.Setup(x => x.GetCustomRepository<ISchoolRepository>())
            .Returns(_schoolRepoMock.Object);

        _handler = new GetMySchoolQueryHandler(_unitOfWorkMock.Object);
    }

    #endregion

    #region GetMySchoolQueryHandler Tests

    [Test]
    public async Task Handle_ShouldReturnSchoolResponse_WhenUserAndSchoolExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var school = new School { Id = 1, Name = "Test School" };
        var user = new ApplicationUser { Id = userId, SchoolId = school.Id };

        _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _schoolRepoMock.Setup(x => x.GetByIdAsync(school.Id)).ReturnsAsync(school);

        var query = new GetMySchoolQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("Test School"));
    }

    [Test]
    public void Handle_ShouldThrowUserNotExistsException_WhenUserNotFound()
    {
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null);

        var query = new GetMySchoolQuery(userId);

        Assert.ThrowsAsync<UserNotExistsException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Test]
    public void Handle_ShouldThrowUserNotPartOfSchoolException_WhenSchoolIdIsNull()
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, SchoolId = null };
        _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        var query = new GetMySchoolQuery(userId);

        Assert.ThrowsAsync<UserNotPartOfSchoolException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Test]
    public void Handle_ShouldThrowSchoolNotFoundException_WhenSchoolNotFound()
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, SchoolId = 123 };
        _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _schoolRepoMock.Setup(x => x.GetByIdAsync(123)).ReturnsAsync((School?)null);

        var query = new GetMySchoolQuery(userId);

        Assert.ThrowsAsync<SchoolNotFoundException>(() => _handler.Handle(query, CancellationToken.None));
    }

    #endregion

}