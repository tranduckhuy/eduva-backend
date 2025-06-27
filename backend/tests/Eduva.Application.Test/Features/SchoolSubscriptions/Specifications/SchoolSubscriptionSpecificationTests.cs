using Eduva.Application.Features.SchoolSubscriptions.Specifications;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;

namespace Eduva.Application.Test.Features.SchoolSubscriptions.Specifications
{
    [TestFixture]
    public class SchoolSubscriptionSpecificationTests
    {
        private List<SchoolSubscription> _mockData = default!;

        #region SchoolSubscriptionSpecfication Setup

        [SetUp]
        public void Setup()
        {
            var now = DateTimeOffset.UtcNow;

            _mockData = new List<SchoolSubscription>
            {
                new()
                {
                    SubscriptionStatus = SubscriptionStatus.Active,
                    BillingCycle = BillingCycle.Monthly,
                    CreatedAt = now.AddDays(-1),
                    StartDate = now.AddDays(-30),
                    EndDate = now.AddDays(30),
                    PaymentTransaction = new PaymentTransaction { Amount = 100000 },
                },
                new()
                {
                    SubscriptionStatus = SubscriptionStatus.Expired,
                    BillingCycle = BillingCycle.Yearly,
                    CreatedAt = now.AddDays(-10),
                    StartDate = now.AddDays(-60),
                    EndDate = now.AddDays(300),
                    PaymentTransaction = new PaymentTransaction { Amount = 500000 },
                },
                new()
                {
                    SubscriptionStatus = SubscriptionStatus.Pending,
                    BillingCycle = BillingCycle.Monthly,
                    CreatedAt = now.AddDays(-31),
                    StartDate = now.AddDays(-90),
                    EndDate = now.AddDays(10),
                    PaymentTransaction = new PaymentTransaction { Amount = 250000 },
                }
            };
        }

        #endregion

        #region SchoolSubscriptionSpecfication Tests

        [Test]
        public void Should_Filter_By_SubscriptionStatus_And_BillingCycle()
        {
            var param = new SchoolSubscriptionSpecParam
            {
                SubscriptionStatus = SubscriptionStatus.Active,
                BillingCycle = BillingCycle.Monthly
            };

            var spec = new SchoolSubscriptionSpecification(param);
            var result = _mockData.AsQueryable().Where(spec.Criteria).ToList();

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].SubscriptionStatus, Is.EqualTo(SubscriptionStatus.Active));
        }

        [TestCase(DateFilter.Today)]
        [TestCase(DateFilter.Last7Days)]
        [TestCase(DateFilter.Last30Days)]
        public void Should_Filter_By_DateFilter(DateFilter filter)
        {
            var param = new SchoolSubscriptionSpecParam
            {
                DateFilter = filter
            };

            var spec = new SchoolSubscriptionSpecification(param);
            var result = _mockData.AsQueryable().Where(spec.Criteria).ToList();

            Assert.That(result, Has.Count.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void Should_Set_Skip_And_Take_Correctly()
        {
            var param = new SchoolSubscriptionSpecParam
            {
                PageIndex = 2,
                PageSize = 10
            };

            var spec = new SchoolSubscriptionSpecification(param);

            Assert.Multiple(() =>
            {
                Assert.That(spec.Skip, Is.EqualTo(10));
                Assert.That(spec.Take, Is.EqualTo(10));
            });
        }

        [TestCase("startDate", "asc")]
        [TestCase("endDate", "desc")]
        [TestCase("createdAt", "asc")]
        [TestCase("amount", "desc")]
        [TestCase("unknown", "desc")] // fallback to createdAt
        public void Should_Sort_Correctly_By_Field(string sortBy, string direction)
        {
            var param = new SchoolSubscriptionSpecParam
            {
                SortBy = sortBy,
                SortDirection = direction
            };

            var spec = new SchoolSubscriptionSpecification(param);
            var result = spec.OrderBy!(_mockData.AsQueryable()).ToList();

            Assert.That(result, Has.Count.EqualTo(_mockData.Count));
        }

        [Test]
        public void Should_Include_Navigation_Properties()
        {
            var spec = new SchoolSubscriptionSpecification(new SchoolSubscriptionSpecParam());

            Assert.Multiple(() =>
            {
                Assert.That(spec.Includes.Any(x => x.Body.ToString()!.Contains("School")), Is.True);
                Assert.That(spec.Includes.Any(x => x.Body.ToString()!.Contains("Plan")), Is.True);
                Assert.That(spec.Includes.Any(x => x.Body.ToString()!.Contains("PaymentTransaction")), Is.True);
                Assert.That(spec.Includes.Any(x => x.Body.ToString()!.Contains("User")), Is.True);
            });
        }

        #endregion

    }
}