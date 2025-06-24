using Eduva.Application.Common.Exceptions;
using Eduva.Application.Exceptions.School;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Services;
using Moq;
using System.Linq.Expressions;

namespace Eduva.Infrastructure.Test.Services
{
    [TestFixture]
    public class SchoolValidationServiceTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IGenericRepository<School, int>> _schoolRepoMock = null!;
        private Mock<IUserRepository> _userRepoMock = null!;
        private SchoolValidationService _service = null!;

        #region SchoolValidationServiceTest Setup

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _schoolRepoMock = new Mock<IGenericRepository<School, int>>();
            _userRepoMock = new Mock<IUserRepository>();

            _unitOfWorkMock.Setup(x => x.GetRepository<School, int>()).Returns(_schoolRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetCustomRepository<IUserRepository>()).Returns(_userRepoMock.Object);

            _service = new SchoolValidationService(_unitOfWorkMock.Object);
        }

        #endregion

        #region SchoolValidationService Tests

        [Test]
        public void Should_Throw_SchoolNotFoundException_When_School_Not_Found()
        {
            _schoolRepoMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<School, bool>>>(),
                It.IsAny<Func<IQueryable<School>, IQueryable<School>>>(),
                It.IsAny<CancellationToken>())).ReturnsAsync((School?)null);

            Assert.ThrowsAsync<SchoolNotFoundException>(() => _service.ValidateCanAddUsersAsync(1));
        }

        [Test]
        public void Should_Throw_AppException_When_School_Is_Inactive()
        {
            var school = new School { Id = 1, Status = EntityStatus.Inactive };
            _schoolRepoMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<School, bool>>>(),
                It.IsAny<Func<IQueryable<School>, IQueryable<School>>>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(school);

            Assert.ThrowsAsync<AppException>(() => _service.ValidateCanAddUsersAsync(1));
        }

        [Test]
        public void Should_Throw_AppException_When_No_Active_Subscription()
        {
            var school = new School
            {
                Id = 1,
                Status = EntityStatus.Active,
                SchoolSubscriptions = new List<SchoolSubscription>
                {
                    new SchoolSubscription { SubscriptionStatus = SubscriptionStatus.Expired }
                }
            };

            _schoolRepoMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<School, bool>>>(),
                It.IsAny<Func<IQueryable<School>, IQueryable<School>>>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(school);

            Assert.ThrowsAsync<AppException>(() => _service.ValidateCanAddUsersAsync(1));
        }

        [Test]
        public void Should_Throw_AppException_When_Plan_Is_Null()
        {
            var school = new School
            {
                Id = 1,
                Status = EntityStatus.Active,
                SchoolSubscriptions = new List<SchoolSubscription>
                {
                    new SchoolSubscription
                    {
                        SubscriptionStatus = SubscriptionStatus.Active,
                        Plan = null!
                    }
                }
            };

            _schoolRepoMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<School, bool>>>(),
                It.IsAny<Func<IQueryable<School>, IQueryable<School>>>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(school);

            Assert.ThrowsAsync<AppException>(() => _service.ValidateCanAddUsersAsync(1));
        }

        [Test]
        public void Should_Throw_AppException_When_MaxUsers_Is_Zero()
        {
            var school = new School
            {
                Id = 1,
                Status = EntityStatus.Active,
                SchoolSubscriptions = new List<SchoolSubscription>
                {
                    new SchoolSubscription
                    {
                        SubscriptionStatus = SubscriptionStatus.Active,
                        Plan = new SubscriptionPlan { MaxUsers = 0 }
                    }
                }
            };

            _schoolRepoMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<School, bool>>>(),
                It.IsAny<Func<IQueryable<School>, IQueryable<School>>>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(school);

            Assert.ThrowsAsync<AppException>(() => _service.ValidateCanAddUsersAsync(1));
        }

        [Test]
        public void Should_Throw_AppException_When_UserCount_Exceeds_Max()
        {
            var school = new School
            {
                Id = 1,
                Status = EntityStatus.Active,
                SchoolSubscriptions = new List<SchoolSubscription>
                {
                    new SchoolSubscription
                    {
                        SubscriptionStatus = SubscriptionStatus.Active,
                        Plan = new SubscriptionPlan { MaxUsers = 10 }
                    }
                }
            };

            _schoolRepoMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<School, bool>>>(),
                It.IsAny<Func<IQueryable<School>, IQueryable<School>>>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(school);

            _userRepoMock.Setup(x => x.CountAsync(
                It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(10); // Max already reached

            Assert.ThrowsAsync<AppException>(() => _service.ValidateCanAddUsersAsync(1));
        }

        [Test]
        public async Task Should_Pass_Validation_When_Within_Limit()
        {
            var school = new School
            {
                Id = 1,
                Status = EntityStatus.Active,
                SchoolSubscriptions = new List<SchoolSubscription>
        {
            new SchoolSubscription
            {
                SubscriptionStatus = SubscriptionStatus.Active,
                Plan = new SubscriptionPlan { MaxUsers = 20 }
            }
        }
            };

            _schoolRepoMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<School, bool>>>(),
                It.IsAny<Func<IQueryable<School>, IQueryable<School>>>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(school);

            _userRepoMock.Setup(x => x.CountAsync(
                It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(10);

            Assert.DoesNotThrowAsync(() => _service.ValidateCanAddUsersAsync(1, 5));
            await _service.ValidateCanAddUsersAsync(1, 5);
        }

        #endregion

    }
}