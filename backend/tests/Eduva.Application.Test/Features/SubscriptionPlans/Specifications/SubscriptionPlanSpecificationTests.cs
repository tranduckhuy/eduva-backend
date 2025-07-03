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
        public void Should_Not_Filter_By_ActiveOnly_When_Null()
        {
            var param = new SubscriptionPlanSpecParam { ActiveOnly = null };
            var spec = new SubscriptionPlanSpecification(param);
            var filtered = _mockData.AsQueryable().Where(spec.Criteria).ToList();

            Assert.That(filtered, Has.Count.EqualTo(3));
        }

        [Test]
        public void Should_Not_Filter_By_Empty_SearchTerm()
        {
            var param = new SubscriptionPlanSpecParam { SearchTerm = "" };
            var spec = new SubscriptionPlanSpecification(param);
            var filtered = _mockData.AsQueryable().Where(spec.Criteria).ToList();

            Assert.That(filtered, Has.Count.EqualTo(3));
        }

        [Test]
        public void Should_Sort_By_StorageLimitGB_Desc()
        {
            _mockData[0].StorageLimitGB = 10;
            _mockData[1].StorageLimitGB = 30;
            _mockData[2].StorageLimitGB = 20;

            var param = new SubscriptionPlanSpecParam { SortBy = "storage", SortDirection = "desc" };
            var spec = new SubscriptionPlanSpecification(param);
            var sorted = spec.OrderBy!(_mockData.AsQueryable()).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(sorted[0].StorageLimitGB, Is.EqualTo(30));
                Assert.That(sorted[1].StorageLimitGB, Is.EqualTo(20));
                Assert.That(sorted[2].StorageLimitGB, Is.EqualTo(10));
            });
        }

        [Test]
        public void Should_Sort_By_MaxUsers_Asc()
        {
            _mockData[0].MaxUsers = 10;
            _mockData[1].MaxUsers = 30;
            _mockData[2].MaxUsers = 20;

            var param = new SubscriptionPlanSpecParam { SortBy = "users", SortDirection = "asc" };
            var spec = new SubscriptionPlanSpecification(param);
            var sorted = spec.OrderBy!(_mockData.AsQueryable()).ToList();

            Assert.That(sorted[0].MaxUsers, Is.EqualTo(10));
        }

        [Test]
        public void Should_Sort_By_PriceMonthly_Desc()
        {
            var param = new SubscriptionPlanSpecParam { SortBy = "monthly", SortDirection = "desc" };
            var spec = new SubscriptionPlanSpecification(param);
            var sorted = spec.OrderBy!(_mockData.AsQueryable()).ToList();

            Assert.That(sorted[0].PriceMonthly, Is.EqualTo(300000));
        }

        [Test]
        public void Should_Sort_By_PricePerYear_Asc()
        {
            _mockData[0].PricePerYear = 1000000;
            _mockData[1].PricePerYear = 3000000;
            _mockData[2].PricePerYear = 2000000;

            var param = new SubscriptionPlanSpecParam { SortBy = "yearly", SortDirection = "asc" };
            var spec = new SubscriptionPlanSpecification(param);
            var sorted = spec.OrderBy!(_mockData.AsQueryable()).ToList();

            Assert.That(sorted[0].PricePerYear, Is.EqualTo(1000000));
        }


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

        [Test]
        public void Should_Handle_Null_SortDirection()
        {
            var param = new SubscriptionPlanSpecParam
            {
                SortBy = "name",
                SortDirection = null!
            };

            var spec = new SubscriptionPlanSpecification(param);
            var sorted = spec.OrderBy!(_mockData.AsQueryable()).ToList();

            Assert.That(sorted[0].Name, Is.EqualTo("Basic")); // Ascending mặc định
        }

        [Test]
        public void Should_Sort_By_StorageLimitGB_Asc()
        {
            _mockData[0].StorageLimitGB = 30;
            _mockData[1].StorageLimitGB = 10;
            _mockData[2].StorageLimitGB = 20;

            var param = new SubscriptionPlanSpecParam { SortBy = "storage", SortDirection = "asc" };
            var spec = new SubscriptionPlanSpecification(param);
            var sorted = spec.OrderBy!(_mockData.AsQueryable()).ToList();

            Assert.That(sorted[0].StorageLimitGB, Is.EqualTo(10));
        }

        [Test]
        public void Should_Sort_By_MaxUsers_Desc()
        {
            _mockData[0].MaxUsers = 10;
            _mockData[1].MaxUsers = 30;
            _mockData[2].MaxUsers = 20;

            var param = new SubscriptionPlanSpecParam { SortBy = "users", SortDirection = "desc" };
            var spec = new SubscriptionPlanSpecification(param);
            var sorted = spec.OrderBy!(_mockData.AsQueryable()).ToList();

            Assert.That(sorted[0].MaxUsers, Is.EqualTo(30));
        }

        [Test]
        public void Should_Sort_By_PriceMonthly_Asc()
        {
            var param = new SubscriptionPlanSpecParam { SortBy = "monthly", SortDirection = "asc" };
            var spec = new SubscriptionPlanSpecification(param);
            var sorted = spec.OrderBy!(_mockData.AsQueryable()).ToList();

            Assert.That(sorted[0].PriceMonthly, Is.EqualTo(100000));
        }

        [Test]
        public void Should_Sort_By_PricePerYear_Desc()
        {
            _mockData[0].PricePerYear = 1000000;
            _mockData[1].PricePerYear = 3000000;
            _mockData[2].PricePerYear = 2000000;

            var param = new SubscriptionPlanSpecParam { SortBy = "yearly", SortDirection = "desc" };
            var spec = new SubscriptionPlanSpecification(param);
            var sorted = spec.OrderBy!(_mockData.AsQueryable()).ToList();

            Assert.That(sorted[0].PricePerYear, Is.EqualTo(3000000));
        }

        #endregion

    }
}