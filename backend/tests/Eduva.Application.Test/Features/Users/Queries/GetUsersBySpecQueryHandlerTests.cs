using AutoMapper;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Users.Queries;
using Eduva.Application.Features.Users.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Eduva.Application.Test.Features.Users.Queries;

[TestFixture]
public class GetUsersBySpecQueryHandlerTests
{
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<UserManager<ApplicationUser>> _userManagerMock = default!;
    private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepoMock = default!;
    private GetUsersBySpecQueryHandler _handler = default!;

    #region GetUsersBySpecQueryHandlerTests Setup

    [SetUp]
    public void Setup()
    {
        _userRepoMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(x => x.GetRepository<ApplicationUser, Guid>())
                       .Returns(_userRepoMock.Object);

        var storeMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            storeMock.Object,
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<ApplicationUser>>(),
            new List<IUserValidator<ApplicationUser>>(),
            new List<IPasswordValidator<ApplicationUser>>(),
            Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<ApplicationUser>>>()
        );

        var config = new MapperConfiguration(cfg => cfg.AddProfile<AppMappingProfile>());
        _handler = new GetUsersBySpecQueryHandler(_unitOfWorkMock.Object, _userManagerMock.Object);
    }

    #endregion

    #region GetUsersBySpecQueryHandler Tests

    [Test]
    public async Task Should_Return_All_Users_When_Role_Not_Specified()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), FullName = "NoRoleCheck" };

        _userRepoMock.Setup(repo => repo.GetWithSpecAsync(It.IsAny<UserSpecification>()))
            .ReturnsAsync(new Pagination<ApplicationUser>
            {
                PageIndex = 1,
                PageSize = 10,
                Count = 1,
                Data = [user]
            });

        _userManagerMock.Setup(mgr => mgr.GetRolesAsync(user))
            .ReturnsAsync(["Teacher"]);

        var query = new GetUsersBySpecQuery(new UserSpecParam());
        var result = await _handler.Handle(query, default);

        Assert.That(result.Data, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(result.Data.First().FullName, Is.EqualTo(user.FullName));
            Assert.That(result.Data.First().Roles, Contains.Item("Teacher"));
        });
    }

    [Test]
    public async Task Should_Filter_Users_With_Matching_Role()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), FullName = "MatchRole" };

        _userRepoMock.Setup(repo => repo.GetWithSpecAsync(It.IsAny<UserSpecification>()))
            .ReturnsAsync(new Pagination<ApplicationUser>
            {
                PageIndex = 1,
                PageSize = 10,
                Count = 1,
                Data = new List<ApplicationUser> { user }
            });

        _userManagerMock.Setup(mgr => mgr.GetRolesAsync(user))
            .ReturnsAsync([Role.Teacher.ToString()]);

        var query = new GetUsersBySpecQuery(new UserSpecParam { Role = Role.Teacher });
        var result = await _handler.Handle(query, default);

        Assert.That(result.Data, Has.Count.EqualTo(1));
        Assert.That(result.Data.First().Roles, Contains.Item("Teacher"));
    }

    [Test]
    public async Task Should_Exclude_Users_Without_Matching_Role()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), FullName = "WrongRole" };

        _userRepoMock.Setup(repo => repo.GetWithSpecAsync(It.IsAny<UserSpecification>()))
            .ReturnsAsync(new Pagination<ApplicationUser>
            {
                PageIndex = 1,
                PageSize = 10,
                Count = 1,
                Data = [user]
            });

        _userManagerMock.Setup(mgr => mgr.GetRolesAsync(user))
            .ReturnsAsync([Role.Student.ToString()]);

        var query = new GetUsersBySpecQuery(new UserSpecParam { Role = Role.Teacher });
        var result = await _handler.Handle(query, default);

        Assert.That(result.Data, Is.Empty);
    }

    [Test]
    public async Task Should_Return_Empty_When_No_Users_Found()
    {
        _userRepoMock.Setup(repo => repo.GetWithSpecAsync(It.IsAny<UserSpecification>()))
            .ReturnsAsync(new Pagination<ApplicationUser>
            {
                PageIndex = 1,
                PageSize = 10,
                Count = 0,
                Data = []
            });

        var query = new GetUsersBySpecQuery(new UserSpecParam());
        var result = await _handler.Handle(query, default);

        Assert.That(result.Count, Is.EqualTo(0));
        Assert.That(result.Data, Is.Empty);
    }

    #endregion

}