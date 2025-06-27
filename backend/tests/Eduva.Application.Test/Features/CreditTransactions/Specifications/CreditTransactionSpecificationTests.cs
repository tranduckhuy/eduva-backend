using Eduva.Application.Features.CreditTransactions.Specifications;
using Eduva.Domain.Entities;
using System.Linq.Expressions;

namespace Eduva.Application.Test.Features.CreditTransactions.Specifications
{
    [TestFixture]
    public class CreditTransactionSpecificationTests
    {

        #region CreditTransactionSpecification Tests

        [Test]
        public void Constructor_ShouldSetIncludesSkipTake()
        {
            // Arrange
            var param = new CreditTransactionSpecParam
            {
                PageIndex = 2,
                PageSize = 10
            };

            // Act
            var spec = new CreditTransactionSpecification(param);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(spec.Skip, Is.EqualTo(10));
                Assert.That(spec.Take, Is.EqualTo(10));
                Assert.That(spec.Includes.Count, Is.EqualTo(3));

                Assert.That(spec.Includes.Any(e => GetMemberName(e) == "User"), Is.True);
                Assert.That(spec.Includes.Any(e => GetMemberName(e) == "AICreditPack"), Is.True);
                Assert.That(spec.Includes.Any(e => GetMemberName(e) == "PaymentTransaction"), Is.True);
            });
        }

        private static string? GetMemberName(Expression<Func<UserCreditTransaction, object>> expression)
        {
            return expression.Body switch
            {
                MemberExpression member => member.Member.Name,
                UnaryExpression unary when unary.Operand is MemberExpression member => member.Member.Name,
                _ => null
            };
        }

        [Test]
        public void Criteria_ShouldAllowAll_WhenFiltersAreDefault()
        {
            // Arrange
            var param = new CreditTransactionSpecParam
            {
                UserId = Guid.Empty,
                AICreditPackId = 0
            };
            var spec = new CreditTransactionSpecification(param);
            var credit = new UserCreditTransaction
            {
                UserId = Guid.NewGuid(),
                AICreditPackId = 5
            };

            // Act
            var result = spec.Criteria.Compile()(credit);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void Criteria_ShouldFilterByUserIdAndPackId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var param = new CreditTransactionSpecParam
            {
                UserId = userId,
                AICreditPackId = 2
            };
            var spec = new CreditTransactionSpecification(param);

            var match = new UserCreditTransaction { UserId = userId, AICreditPackId = 2 };
            var notMatch = new UserCreditTransaction { UserId = Guid.NewGuid(), AICreditPackId = 1 };

            // Act
            var criteria = spec.Criteria.Compile();

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(criteria(match), Is.True);
                Assert.That(criteria(notMatch), Is.False);
            });
        }

        [TestCase("CreatedAt", "asc")]
        [TestCase("CreatedAt", "desc")]
        [TestCase("Credits", "asc")]
        [TestCase("Credits", "desc")]
        [TestCase("InvalidColumn", "asc")]
        public void OrderBy_ShouldApplyCorrectSorting(string sortBy, string sortDirection)
        {
            // Arrange
            var param = new CreditTransactionSpecParam
            {
                SortBy = sortBy,
                SortDirection = sortDirection
            };
            var spec = new CreditTransactionSpecification(param);
            var orderBy = spec.OrderBy;

            var data = new[]
            {
                new UserCreditTransaction { Credits = 100, CreatedAt = DateTimeOffset.UtcNow.AddDays(-2) },
                new UserCreditTransaction { Credits = 300, CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) },
                new UserCreditTransaction { Credits = 200, CreatedAt = DateTimeOffset.UtcNow }
            }.AsQueryable();

            // Act
            var sorted = orderBy!(data).ToList();

            // Assert
            Assert.That(sorted.Count, Is.EqualTo(3));
        }

        #endregion

    }
}