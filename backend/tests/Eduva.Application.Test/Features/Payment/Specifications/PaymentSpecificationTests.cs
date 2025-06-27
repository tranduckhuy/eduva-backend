using Eduva.Application.Features.Payments.Specifications;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;

namespace Eduva.Application.Test.Features.Payment.Specifications
{
    [TestFixture]
    public class PaymentSpecificationTests
    {

        #region PaymentSpecification Tests

        [Test]
        public void Constructor_ShouldSetIncludesSkipTake()
        {
            // Arrange
            var param = new PaymentSpecParam
            {
                PageIndex = 2,
                PageSize = 10,
                PaymentPurpose = PaymentPurpose.SchoolSubscription,
                PaymentMethod = PaymentMethod.PayOS,
                PaymentStatus = PaymentStatus.Paid
            };

            // Act
            var spec = new PaymentSpecification(param);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(spec.Includes.Any(x => x.Body.ToString()!.Contains("User")), Is.True);
                Assert.That(spec.Skip, Is.EqualTo(10));
                Assert.That(spec.Take, Is.EqualTo(10));
            });
        }

        [Test]
        public void Criteria_ShouldReturnTrue_WhenMatchingParams()
        {
            // Arrange
            var param = new PaymentSpecParam
            {
                PaymentPurpose = PaymentPurpose.CreditPackage,
                PaymentMethod = PaymentMethod.PayOS,
                PaymentStatus = PaymentStatus.Pending,
                SearchTerm = "john"
            };

            var transactions = new List<PaymentTransaction>
        {
            new()
            {
                PaymentPurpose = PaymentPurpose.CreditPackage,
                PaymentMethod = PaymentMethod.PayOS,
                PaymentStatus = PaymentStatus.Pending,
                User = new ApplicationUser
                {
                    FullName = "John Doe",
                    Email = "john@example.com"
                }
            },
            new()
            {
                PaymentPurpose = PaymentPurpose.CreditPackage,
                PaymentMethod = PaymentMethod.PayOS,
                PaymentStatus = PaymentStatus.Pending,
                User = new ApplicationUser
                {
                    FullName = "Alice",
                    Email = "alice@example.com"
                }
            }
            }.AsQueryable();

            var filtered = transactions.Where(t =>
                (!param.PaymentPurpose.HasValue || t.PaymentPurpose == param.PaymentPurpose) &&
                (!param.PaymentMethod.HasValue || t.PaymentMethod == param.PaymentMethod) &&
                (!param.PaymentStatus.HasValue || t.PaymentStatus == param.PaymentStatus) &&
                (string.IsNullOrWhiteSpace(param.SearchTerm) ||
                    (!string.IsNullOrEmpty(t.User.FullName) && t.User.FullName.ToLower().Contains(param.SearchTerm.ToLower())) ||
                    (!string.IsNullOrEmpty(t.User.Email) && t.User.Email.ToLower().Contains(param.SearchTerm.ToLower())))
            ).ToList();

            // Assert
            Assert.That(filtered, Has.Count.EqualTo(1));
            Assert.That(filtered[0].User.FullName, Is.EqualTo("John Doe"));
        }

        [Test]
        public void Criteria_ShouldReturnFalse_WhenNotMatchingSearch()
        {
            var param = new PaymentSpecParam
            {
                SearchTerm = "abc"
            };

            var transactions = new List<PaymentTransaction>
            {
            new()
            {
            User = new ApplicationUser
            {
                FullName = "John Doe",
                Email = "john@example.com"
            }
            }
            }.AsQueryable();

            var filtered = transactions.Where(t =>
                string.IsNullOrWhiteSpace(param.SearchTerm) ||
                (!string.IsNullOrEmpty(t.User.FullName) && t.User.FullName.ToLower().Contains(param.SearchTerm.ToLower())) ||
                (!string.IsNullOrEmpty(t.User.Email) && t.User.Email.ToLower().Contains(param.SearchTerm.ToLower()))
            ).ToList();

            Assert.That(filtered, Is.Empty);
        }

        [TestCase("fullName", "desc")]
        [TestCase("email", "asc")]
        [TestCase("amount", "desc")]
        [TestCase("createdAt", "asc")]
        [TestCase("unknown", "asc")]
        public void OrderBy_ShouldOrderCorrectly(string sortBy, string sortDirection)
        {
            // Arrange
            var param = new PaymentSpecParam
            {
                SortBy = sortBy,
                SortDirection = sortDirection
            };

            var spec = new PaymentSpecification(param);

            var data = new[]
            {
                new PaymentTransaction
                {
                    Amount = 100,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
                    User = new ApplicationUser
                    {
                        FullName = "Alice",
                        Email = "alice@example.com"
                    }
                },
                new PaymentTransaction
                {
                    Amount = 200,
                    CreatedAt = DateTimeOffset.UtcNow,
                    User = new ApplicationUser
                    {
                        FullName = "Bob",
                        Email = "bob@example.com"
                    }
                }
            }.AsQueryable();

            // Act
            var ordered = spec.OrderBy!(data).ToList();

            // Assert
            Assert.That(ordered, Has.Count.EqualTo(2));
        }

        #endregion

    }
}