using Eduva.Application.Features.SubscriptionPlans.Specifications;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;

namespace Eduva.Application.Test.Features.SubscriptionPlans.Specifications
{
    [TestFixture]
    public class SubscriptionPlanSpecificationTests
    {
        private List<SubscriptionPlan> _mockData = default!;

        #region SubscriptionPlanSpecification Setup

        [SetUp]
        public void Setup()
        {
            _mockData = new List<SubscriptionPlan>
            {
                new SubscriptionPlan { Name = "Basic", Status = EntityStatus.Active, PriceMonthly = 100000, CreatedAt = DateTime.Now.AddDays(-3) },
                new SubscriptionPlan { Name = "Premium", Status = EntityStatus.Inactive, PriceMonthly = 200000, CreatedAt = DateTime.Now.AddDays(-2) },
                new SubscriptionPlan { Name = "Plus", Status = EntityStatus.Active, PriceMonthly = 300000, CreatedAt = DateTime.Now.AddDays(-1) }
            };
        }

        #endregion

        #region SubscriptionPlanSpecification Tests

        [Test]
        public void Should_Set_Pagination_Skip_And_Take()
        {
            var param = new SubscriptionPlanSpecParam { PageIndex = 2, PageSize = 10 };
            var spec = new SubscriptionPlanSpecification(param);

            Assert.Multiple(() =>
            {
                Assert.That(spec.Skip, Is.EqualTo(10));
                Assert.That(spec.Take, Is.EqualTo(10));
            });
        }

        [Test]
        public void Should_Filter_By_ActiveOnly_True()
        {
            var param = new SubscriptionPlanSpecParam { ActiveOnly = true };
            var spec = new SubscriptionPlanSpecification(param);
            var filtered = _mockData.AsQueryable().Where(spec.Criteria).ToList();

            Assert.That(filtered.All(p => p.Status == EntityStatus.Active));
        }

        [Test]
        public void Should_Filter_By_ActiveOnly_False()
        {
            var param = new SubscriptionPlanSpecParam { ActiveOnly = false };
            var spec = new SubscriptionPlanSpecification(param);
            var filtered = _mockData.AsQueryable().Where(spec.Criteria).ToList();

            Assert.That(filtered.All(p => p.Status != EntityStatus.Active));
        }

        [Test]
        public void Should_Filter_By_SearchTerm()
        {
            var param = new SubscriptionPlanSpecParam { SearchTerm = "basic" };
            var searchTerm = param.SearchTerm.ToLower();

            var filtered = _mockData
                .Where(p =>
                    string.IsNullOrWhiteSpace(param.SearchTerm) ||
                    p.Name.ToLower().Contains(searchTerm))
                .ToList();

            Assert.That(filtered, Has.Count.EqualTo(1));
            Assert.That(filtered[0].Name, Is.EqualTo("Basic"));
        }

        [Test]
        public void Should_Sort_By_Name_Asc()
        {
            var param = new SubscriptionPlanSpecParam { SortBy = "name", SortDirection = "asc" };
            var spec = new SubscriptionPlanSpecification(param);
            var sorted = spec.OrderBy!(_mockData.AsQueryable()).ToList();

            Assert.That(sorted[0].Name, Is.EqualTo("Basic"));
        }

        [Test]
        public void Should_Sort_By_Name_Desc()
        {
            var param = new SubscriptionPlanSpecParam { SortBy = "name", SortDirection = "desc" };
            var spec = new SubscriptionPlanSpecification(param);
            var sorted = spec.OrderBy!(_mockData.AsQueryable()).ToList();

            Assert.That(sorted[0].Name, Is.EqualTo("Premium"));
        }

        [Test]
        public void Should_Sort_By_Price_Asc()
        {
            var param = new SubscriptionPlanSpecParam { SortBy = "price", SortDirection = "asc" };
            var spec = new SubscriptionPlanSpecification(param);
            var sorted = spec.OrderBy!(_mockData.AsQueryable()).ToList();

            Assert.That(sorted[0].PriceMonthly, Is.EqualTo(100000));
        }

        [Test]
        public void Should_Sort_By_Price_Desc()
        {
            var param = new SubscriptionPlanSpecParam { SortBy = "price", SortDirection = "desc" };
            var spec = new SubscriptionPlanSpecification(param);
            var sorted = spec.OrderBy!(_mockData.AsQueryable()).ToList();

            Assert.That(sorted[0].PriceMonthly, Is.EqualTo(300000));
        }

        [Test]
        public void Should_Sort_By_Default_CreatedAt_Asc()
        {
            var param = new SubscriptionPlanSpecParam { SortBy = "unknown", SortDirection = "asc" };
            var spec = new SubscriptionPlanSpecification(param);
            var sorted = spec.OrderBy!(_mockData.AsQueryable()).ToList();

            Assert.That(sorted[0].CreatedAt, Is.LessThan(sorted[2].CreatedAt));
        }

        [Test]
        public void Should_Sort_By_Default_CreatedAt_Desc()
        {
            var param = new SubscriptionPlanSpecParam { SortBy = "unknown", SortDirection = "desc" };
            var spec = new SubscriptionPlanSpecification(param);
            var sorted = spec.OrderBy!(_mockData.AsQueryable()).ToList();

            Assert.That(sorted[0].CreatedAt, Is.GreaterThan(sorted[2].CreatedAt));
        }

        [Test]
        public void Should_NotApplyOrdering_When_SortBy_Null()
        {
            var param = new SubscriptionPlanSpecParam { SortBy = null };
            var spec = new SubscriptionPlanSpecification(param);

            Assert.That(spec.OrderBy, Is.Null);
        }

        [Test]
        public void Should_Project_Using_Selector()
        {
            // Arrange
            var param = new SubscriptionPlanSpecParam();
            var selectorFunc = new Func<IQueryable<SubscriptionPlan>, IQueryable<SubscriptionPlan>>(q =>
                q.Select(x => new SubscriptionPlan
                {
                    Name = x.Name.ToUpper(),
                    PriceMonthly = x.PriceMonthly
                }));

            var spec = new SubscriptionPlanSpecification(param)
            {
                Selector = selectorFunc
            };

            // Act
            var projected = spec.Selector!(_mockData.AsQueryable()).ToList();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(projected, Has.Count.EqualTo(3));
                Assert.That(projected[0].Name, Is.EqualTo(_mockData[0].Name.ToUpper()));
            });
        }

        #endregion

    }
}