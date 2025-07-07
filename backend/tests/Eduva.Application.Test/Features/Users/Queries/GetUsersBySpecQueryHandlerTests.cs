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
    public async Task Should_Filter_By_Status_With_Role()
    {
        // Arrange
        var param = new UserSpecParam { Role = Role.Student, Status = EntityStatus.Active };
        var query = new GetUsersBySpecQuery(param);

        var mockUsers = new List<ApplicationUser>
        {
            new ApplicationUser { Id = Guid.NewGuid(), FullName = "Active Student", Status = EntityStatus.Active },
            new ApplicationUser { Id = Guid.NewGuid(), FullName = "Inactive Student", Status = EntityStatus.Inactive }
        };

        _userManagerMock.Setup(um => um.GetUsersInRoleAsync("Student"))
            .ReturnsAsync(mockUsers);

        foreach (var user in mockUsers)
        {
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
        }

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(1)); // Only Active status
        Assert.That(result.Data, Has.Count.EqualTo(1));
        Assert.That(result.Data.First().Status, Is.EqualTo(EntityStatus.Active));
    }

    [Test]
    public async Task Should_Sort_By_FullName_Ascending_With_Role()
    {
        // Arrange
        var param = new UserSpecParam { Role = Role.Student, SortBy = "fullname", SortDirection = "asc" };
        var query = new GetUsersBySpecQuery(param);

        var mockUsers = new List<ApplicationUser>
        {
            new ApplicationUser { Id = Guid.NewGuid(), FullName = "Zebra Student" },
            new ApplicationUser { Id = Guid.NewGuid(), FullName = "Alpha Student" }
        };

        _userManagerMock.Setup(um => um.GetUsersInRoleAsync("Student"))
            .ReturnsAsync(mockUsers);

        foreach (var user in mockUsers)
        {
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
        }

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.Data.First().FullName, Is.EqualTo("Alpha Student"));
            Assert.That(result.Data.Last().FullName, Is.EqualTo("Zebra Student"));
        });
    }

    [Test]
    public async Task Should_Sort_By_FullName_Descending_With_Role()
    {
        // Arrange
        var param = new UserSpecParam { Role = Role.Teacher, SortBy = "fullname", SortDirection = "desc" };
        var query = new GetUsersBySpecQuery(param);

        var mockUsers = new List<ApplicationUser>
        {
            new ApplicationUser { Id = Guid.NewGuid(), FullName = "Alpha Teacher" },
            new ApplicationUser { Id = Guid.NewGuid(), FullName = "Zebra Teacher" }
        };

        _userManagerMock.Setup(um => um.GetUsersInRoleAsync("Teacher"))
            .ReturnsAsync(mockUsers);

        foreach (var user in mockUsers)
        {
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Teacher" });
        }

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.Data.First().FullName, Is.EqualTo("Zebra Teacher"));
            Assert.That(result.Data.Last().FullName, Is.EqualTo("Alpha Teacher"));
        });
    }

    [Test]
    public async Task Should_Sort_By_Email_Ascending_With_Role()
    {
        // Arrange
        var param = new UserSpecParam { Role = Role.Student, SortBy = "email", SortDirection = "asc" };
        var query = new GetUsersBySpecQuery(param);

        var mockUsers = new List<ApplicationUser>
        {
            new ApplicationUser { Id = Guid.NewGuid(), Email = "zebra@example.com" },
            new ApplicationUser { Id = Guid.NewGuid(), Email = "alpha@example.com" }
        };

        _userManagerMock.Setup(um => um.GetUsersInRoleAsync("Student"))
            .ReturnsAsync(mockUsers);

        foreach (var user in mockUsers)
        {
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
        }

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.Data.First().Email, Is.EqualTo("alpha@example.com"));
            Assert.That(result.Data.Last().Email, Is.EqualTo("zebra@example.com"));
        });
    }

    [Test]
    public async Task Should_Sort_By_Status_Descending_With_Role()
    {
        // Arrange
        var param = new UserSpecParam { Role = Role.Student, SortBy = "status", SortDirection = "desc" };
        var query = new GetUsersBySpecQuery(param);

        var mockUsers = new List<ApplicationUser>
        {
            new ApplicationUser { Id = Guid.NewGuid(), FullName = "Student 1", Status = EntityStatus.Active },
            new ApplicationUser { Id = Guid.NewGuid(), FullName = "Student 2", Status = EntityStatus.Inactive }
        };

        _userManagerMock.Setup(um => um.GetUsersInRoleAsync("Student"))
            .ReturnsAsync(mockUsers);

        foreach (var user in mockUsers)
        {
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
        }

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result.Data.First().Status, Is.GreaterThanOrEqualTo(result.Data.Last().Status));
    }

    [Test]
    public async Task Should_Sort_By_TotalCredits_Ascending_With_Role()
    {
        // Arrange
        var param = new UserSpecParam { Role = Role.Teacher, SortBy = "totalcredits", SortDirection = "asc" };
        var query = new GetUsersBySpecQuery(param);

        var mockUsers = new List<ApplicationUser>
    {
        new ApplicationUser { Id = Guid.NewGuid(), FullName = "Teacher 1", TotalCredits = 100 },
        new ApplicationUser { Id = Guid.NewGuid(), FullName = "Teacher 2", TotalCredits = 50 }
    };

        _userManagerMock.Setup(um => um.GetUsersInRoleAsync("Teacher"))
            .ReturnsAsync(mockUsers);

        foreach (var user in mockUsers)
        {
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Teacher" });
        }

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.Data.First().CreditBalance, Is.EqualTo(50));
            Assert.That(result.Data.Last().CreditBalance, Is.EqualTo(100));
        });
    }

    [Test]
    public async Task Should_Sort_By_TotalCredits_Descending_With_Role()
    {
        // Arrange
        var param = new UserSpecParam { Role = Role.Teacher, SortBy = "totalcredits", SortDirection = "desc" };
        var query = new GetUsersBySpecQuery(param);

        var mockUsers = new List<ApplicationUser>
    {
        new ApplicationUser { Id = Guid.NewGuid(), FullName = "Teacher 1", TotalCredits = 100 },
        new ApplicationUser { Id = Guid.NewGuid(), FullName = "Teacher 2", TotalCredits = 50 }
    };

        _userManagerMock.Setup(um => um.GetUsersInRoleAsync("Teacher"))
            .ReturnsAsync(mockUsers);

        foreach (var user in mockUsers)
        {
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Teacher" });
        }

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.Data.First().CreditBalance, Is.EqualTo(100));
            Assert.That(result.Data.Last().CreditBalance, Is.EqualTo(50));
        });
    }

    [Test]
    public async Task Should_Sort_By_PhoneNumber_Descending_With_Role()
    {
        // Arrange
        var param = new UserSpecParam { Role = Role.Teacher, SortBy = "phonenumber", SortDirection = "desc" };
        var query = new GetUsersBySpecQuery(param);

        var mockUsers = new List<ApplicationUser>
        {
            new ApplicationUser { Id = Guid.NewGuid(), PhoneNumber = "0123456789" },
            new ApplicationUser { Id = Guid.NewGuid(), PhoneNumber = "0987654321" }
        };

        _userManagerMock.Setup(um => um.GetUsersInRoleAsync("Teacher"))
            .ReturnsAsync(mockUsers);

        foreach (var user in mockUsers)
        {
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Teacher" });
        }

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.Data.First().PhoneNumber, Is.EqualTo("0987654321"));
            Assert.That(result.Data.Last().PhoneNumber, Is.EqualTo("0123456789"));
        });
    }

    [Test]
    public async Task Should_Use_Default_Sort_By_FullName_When_Invalid_SortBy()
    {
        // Arrange
        var param = new UserSpecParam { Role = Role.Student, SortBy = "invalidfield", SortDirection = "desc" };
        var query = new GetUsersBySpecQuery(param);

        var mockUsers = new List<ApplicationUser>
        {
            new ApplicationUser { Id = Guid.NewGuid(), FullName = "Alpha Student" },
            new ApplicationUser { Id = Guid.NewGuid(), FullName = "Zebra Student" }
        };

        _userManagerMock.Setup(um => um.GetUsersInRoleAsync("Student"))
            .ReturnsAsync(mockUsers);

        foreach (var user in mockUsers)
        {
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
        }

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Multiple(() =>
        {
            // Assert - Should use default sorting by FullName descending
            Assert.That(result.Data.First().FullName, Is.EqualTo("Zebra Student"));
            Assert.That(result.Data.Last().FullName, Is.EqualTo("Alpha Student"));
        });
    }

    [Test]
    public async Task Should_Sort_By_LastLoginAt_With_Role()
    {
        // Arrange
        var param = new UserSpecParam
        {
            Role = Role.Student,
            SortBy = "lastloginin",
            SortDirection = "desc"
        };
        var query = new GetUsersBySpecQuery(param);

        var now = DateTimeOffset.UtcNow;
        var mockUsers = new List<ApplicationUser>
    {
        new ApplicationUser { Id = Guid.NewGuid(), FullName = "Student 1", LastLoginAt = now.AddHours(-1) },
        new ApplicationUser { Id = Guid.NewGuid(), FullName = "Student 2", LastLoginAt = now.AddHours(-2) }
    };

        _userManagerMock.Setup(um => um.GetUsersInRoleAsync("Student"))
            .ReturnsAsync(mockUsers);

        foreach (var user in mockUsers)
        {
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
        }

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result.Data.First().LastLoginAt, Is.GreaterThan(result.Data.Last().LastLoginAt));
    }

    [Test]
    public async Task Should_Sort_By_CreatedAt_With_Role()
    {
        // Arrange
        var param = new UserSpecParam
        {
            Role = Role.Student,
            SortBy = "createdat",
            SortDirection = "desc"
        };
        var query = new GetUsersBySpecQuery(param);

        var now = DateTimeOffset.UtcNow;
        var mockUsers = new List<ApplicationUser>
    {
        new ApplicationUser { Id = Guid.NewGuid(), FullName = "Student 1", CreatedAt = now.AddDays(-1) },
        new ApplicationUser { Id = Guid.NewGuid(), FullName = "Student 2", CreatedAt = now.AddDays(-2) }
    };

        _userManagerMock.Setup(um => um.GetUsersInRoleAsync("Student"))
            .ReturnsAsync(mockUsers);

        foreach (var user in mockUsers)
        {
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
        }

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result.Data.First().CreatedAt, Is.GreaterThan(result.Data.Last().CreatedAt));
    }

    [Test]
    public async Task Should_Sort_By_LastModifiedAt_With_Role()
    {
        // Arrange
        var param = new UserSpecParam
        {
            Role = Role.Teacher,
            SortBy = "lastmodifiedat",
            SortDirection = "asc"
        };
        var query = new GetUsersBySpecQuery(param);

        var now = DateTimeOffset.UtcNow;
        var mockUsers = new List<ApplicationUser>
    {
        new ApplicationUser { Id = Guid.NewGuid(), FullName = "Teacher 1", LastModifiedAt = now.AddHours(-1) },
        new ApplicationUser { Id = Guid.NewGuid(), FullName = "Teacher 2", LastModifiedAt = now.AddHours(-2) }
    };

        _userManagerMock.Setup(um => um.GetUsersInRoleAsync("Teacher"))
            .ReturnsAsync(mockUsers);

        foreach (var user in mockUsers)
        {
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Teacher" });
        }

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result.Data.First().LastModifiedAt, Is.LessThan(result.Data.Last().LastModifiedAt));
    }

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
        // Arrange
        var param = new UserSpecParam { Role = Role.Student };
        var query = new GetUsersBySpecQuery(param);

        var mockUsers = new List<ApplicationUser>
    {
        new ApplicationUser { Id = Guid.NewGuid(), FullName = "Student 1" },
        new ApplicationUser { Id = Guid.NewGuid(), FullName = "Student 2" }
    };

        _userManagerMock.Setup(um => um.GetUsersInRoleAsync("Student"))
            .ReturnsAsync(mockUsers);

        foreach (var user in mockUsers)
        {
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
        }

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.Data, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task Should_Exclude_Users_Without_Matching_Role()
    {
        // Arrange
        var param = new UserSpecParam { Role = Role.Teacher };
        var query = new GetUsersBySpecQuery(param);

        _userManagerMock.Setup(um => um.GetUsersInRoleAsync("Teacher"))
            .ReturnsAsync(new List<ApplicationUser>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0));
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

    [Test]
    public async Task Should_Handle_Empty_UsersInRole()
    {
        // Arrange
        var param = new UserSpecParam { Role = Role.Student };
        var query = new GetUsersBySpecQuery(param);

        _userManagerMock.Setup(um => um.GetUsersInRoleAsync("Student"))
            .ReturnsAsync(new List<ApplicationUser>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0));
        Assert.That(result.Data, Is.Empty);
    }

    [Test]
    public async Task Should_Apply_School_Filter_With_Role()
    {
        // Arrange
        var param = new UserSpecParam { Role = Role.Student, SchoolId = 1 };
        var query = new GetUsersBySpecQuery(param);

        var mockSchool = new School { Id = 1, Name = "Test School" };
        var mockUsers = new List<ApplicationUser>
    {
        new ApplicationUser { Id = Guid.NewGuid(), FullName = "Student 1", SchoolId = 1 },
        new ApplicationUser { Id = Guid.NewGuid(), FullName = "Student 2", SchoolId = 2 }
    };

        _userManagerMock.Setup(um => um.GetUsersInRoleAsync("Student"))
            .ReturnsAsync(mockUsers);

        foreach (var user in mockUsers)
        {
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
        }

        var schoolRepoMock = new Mock<IGenericRepository<School, int>>();
        schoolRepoMock.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(mockSchool);

        _unitOfWorkMock.Setup(uow => uow.GetRepository<School, int>())
            .Returns(schoolRepoMock.Object);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(1)); // Only SchoolId = 1
        Assert.That(result.Data, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Should_Apply_Search_Filter_With_Role()
    {
        // Arrange
        var param = new UserSpecParam { Role = Role.Student, SearchTerm = "John" };
        var query = new GetUsersBySpecQuery(param);

        var mockUsers = new List<ApplicationUser>
    {
        new ApplicationUser { Id = Guid.NewGuid(), FullName = "John Doe" },
        new ApplicationUser { Id = Guid.NewGuid(), FullName = "Jane Smith" }
    };

        _userManagerMock.Setup(um => um.GetUsersInRoleAsync("Student"))
            .ReturnsAsync(mockUsers);

        foreach (var user in mockUsers)
        {
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
        }

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(1)); // Only "John Doe"
        Assert.That(result.Data, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Should_Apply_Pagination_With_Role()
    {
        // Arrange
        var param = new UserSpecParam { Role = Role.Student, PageIndex = 1, PageSize = 1 };
        var query = new GetUsersBySpecQuery(param);

        var mockUsers = new List<ApplicationUser>
    {
        new ApplicationUser { Id = Guid.NewGuid(), FullName = "Student 1" },
        new ApplicationUser { Id = Guid.NewGuid(), FullName = "Student 2" }
    };

        _userManagerMock.Setup(um => um.GetUsersInRoleAsync("Student"))
            .ReturnsAsync(mockUsers);

        foreach (var user in mockUsers)
        {
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
        }

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(2)); // Total count
        Assert.Multiple(() =>
        {
            Assert.That(result.Data, Has.Count.EqualTo(1)); // Page size
            Assert.That(result.PageIndex, Is.EqualTo(1));
            Assert.That(result.PageSize, Is.EqualTo(1));
        });
    }

    #endregion

}