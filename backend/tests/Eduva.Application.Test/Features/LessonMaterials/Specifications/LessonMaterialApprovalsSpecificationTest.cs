using Eduva.Application.Features.LessonMaterials.Specifications;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;

namespace Eduva.Application.Test.Features.LessonMaterials.Specifications
{
    [TestFixture]
    public class LessonMaterialApprovalsSpecificationTest
    {
        [Test]
        public void Should_Set_Criteria_Correctly()
        {
            var param = new LessonMaterialApprovalsSpecParam
            {
                LessonMaterialId = Guid.NewGuid(),
                ApproverId = Guid.NewGuid(),
                CreatedByUserId = Guid.NewGuid(),
                StatusChangeTo = LessonMaterialStatus.Approved,
                FromDate = DateTimeOffset.UtcNow.AddDays(-1),
                ToDate = DateTimeOffset.UtcNow.AddDays(1),
                SchoolId = 123,
                SearchTerm = "math"
            };

            var lessonMaterial = new LessonMaterial
            {
                Id = param.LessonMaterialId.Value,
                CreatedByUserId = param.CreatedByUserId.Value,
                SchoolId = param.SchoolId.Value,
                Title = "Math for kids"
            };

            var approval = new LessonMaterialApproval
            {
                LessonMaterialId = param.LessonMaterialId.Value,
                ApproverId = param.ApproverId.Value,
                LessonMaterial = lessonMaterial,
                StatusChangeTo = LessonMaterialStatus.Approved,
                CreatedAt = DateTimeOffset.UtcNow,
                Feedback = "Great math lesson"
            };

            var spec = new LessonMaterialApprovalsSpecification(param);

            Assert.That(spec.Criteria.Compile()(approval), Is.True);
        }

        [Test]
        public void Should_Set_Criteria_False_When_Not_Match()
        {
            var param = new LessonMaterialApprovalsSpecParam
            {
                LessonMaterialId = Guid.NewGuid(),
                ApproverId = Guid.NewGuid(),
                CreatedByUserId = Guid.NewGuid(),
                StatusChangeTo = LessonMaterialStatus.Approved,
                FromDate = DateTimeOffset.UtcNow.AddDays(-1),
                ToDate = DateTimeOffset.UtcNow.AddDays(1),
                SchoolId = 123,
                SearchTerm = "science"
            };

            var lessonMaterial = new LessonMaterial
            {
                Id = Guid.NewGuid(),
                CreatedByUserId = Guid.NewGuid(),
                SchoolId = 999,
                Title = "Math for kids"
            };

            var approval = new LessonMaterialApproval
            {
                LessonMaterialId = Guid.NewGuid(),
                ApproverId = Guid.NewGuid(),
                LessonMaterial = lessonMaterial,
                StatusChangeTo = LessonMaterialStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow,
                Feedback = "Great math lesson"
            };

            var spec = new LessonMaterialApprovalsSpecification(param);

            Assert.That(spec.Criteria.Compile()(approval), Is.False);
        }

        [Test]
        public void Should_Include_LessonMaterial_And_Approver()
        {
            var param = new LessonMaterialApprovalsSpecParam();
            var spec = new LessonMaterialApprovalsSpecification(param);

            Assert.That(spec.Includes, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(spec.Includes[0].ToString(), Does.Contain("LessonMaterial"));
                Assert.That(spec.Includes[1].ToString(), Does.Contain("Approver"));
            });
        }

        [TestCase("date", "desc")]
        [TestCase("date", "asc")]
        [TestCase("status", "desc")]
        [TestCase("status", "asc")]
        [TestCase("material", "desc")]
        [TestCase("material", "asc")]
        [TestCase("approver", "desc")]
        [TestCase("approver", "asc")]
        [TestCase("unknown", "desc")]
        [TestCase("unknown", "asc")]
        public void Should_Set_OrderBy_Correctly(string sortBy, string sortDirection)
        {
            var param = new LessonMaterialApprovalsSpecParam
            {
                SortBy = sortBy,
                SortDirection = sortDirection
            };
            var spec = new LessonMaterialApprovalsSpecification(param);

            var queryable = new[]
            {
                new LessonMaterialApproval
                {
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
                    StatusChangeTo = LessonMaterialStatus.Approved,
                    LessonMaterial = new LessonMaterial { Title = "B" },
                    Approver = new ApplicationUser { FullName = "UserB" }
                },
                new LessonMaterialApproval
                {
                    CreatedAt = DateTimeOffset.UtcNow,
                    StatusChangeTo = LessonMaterialStatus.Pending,
                    LessonMaterial = new LessonMaterial { Title = "A" },
                    Approver = new ApplicationUser { FullName = "UserA" }
                }
            }.AsQueryable();

            var ordered = spec.OrderBy!(queryable).ToList();
            Assert.That(ordered, Is.Not.Null);
            Assert.That(ordered, Has.Count.EqualTo(2));
        }

        [Test]
        public void Should_Default_OrderBy_CreatedAt_Desc_When_SortBy_Is_Null()
        {
            var param = new LessonMaterialApprovalsSpecParam();
            var spec = new LessonMaterialApprovalsSpecification(param);

            var queryable = new[]
            {
                new LessonMaterialApproval { CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) },
                new LessonMaterialApproval { CreatedAt = DateTimeOffset.UtcNow }
            }.AsQueryable();

            var ordered = spec.OrderBy!(queryable).ToList();
            Assert.That(ordered[0].CreatedAt, Is.GreaterThan(ordered[1].CreatedAt));
        }

        [TestCase(1, 10, 0, 10)]
        [TestCase(2, 5, 5, 5)]
        [TestCase(0, 0, 0, 1)]
        [TestCase(-1, -1, 0, 1)]
        public void Should_Set_Paging_Correctly(int pageIndex, int pageSize, int expectedSkip, int expectedTake)
        {
            var param = new LessonMaterialApprovalsSpecParam
            {
                PageIndex = pageIndex,
                PageSize = pageSize
            };
            var spec = new LessonMaterialApprovalsSpecification(param);

            Assert.Multiple(() =>
            {
                Assert.That(spec.Skip, Is.EqualTo(expectedSkip));
                Assert.That(spec.Take, Is.EqualTo(expectedTake));
            });
        }
    }
}