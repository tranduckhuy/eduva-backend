using Eduva.Application.Features.LessonMaterials.Specifications;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;

namespace Eduva.Application.Test.Features.LessonMaterials.Specifications
{
    [TestFixture]
    public class PendingLessonMaterialSpecificationTest
    {
        [Test]
        public void Should_Set_Criteria_Correctly()
        {
            var param = new LessonMaterialSpecParam
            {
                SchoolId = 1,
                SearchTerm = "math",
                ContentType = ContentType.Video,
                LessonStatus = LessonMaterialStatus.Pending,
                EntityStatus = EntityStatus.Active
            };

            var lm = new LessonMaterial
            {
                SchoolId = 1,
                Title = "Math for kids",
                ContentType = ContentType.Video,
                LessonStatus = LessonMaterialStatus.Pending,
                Status = EntityStatus.Active
            };

            var spec = new PendingLessonMaterialSpecification(param);

            Assert.That(spec.Criteria.Compile()(lm), Is.True);
        }

        [Test]
        public void Should_Set_Criteria_False_When_Not_Match()
        {
            var param = new LessonMaterialSpecParam
            {
                SchoolId = 2,
                SearchTerm = "science",
                ContentType = ContentType.DOCX,
                LessonStatus = LessonMaterialStatus.Approved,
                EntityStatus = EntityStatus.Deleted
            };

            var lm = new LessonMaterial
            {
                SchoolId = 1,
                Title = "Math for kids",
                ContentType = ContentType.Video,
                LessonStatus = LessonMaterialStatus.Pending,
                Status = EntityStatus.Active
            };

            var spec = new PendingLessonMaterialSpecification(param);

            Assert.That(spec.Criteria.Compile()(lm), Is.False);
        }

        [Test]
        public void Should_Include_CreatedByUser_And_FolderLessonMaterials()
        {
            var param = new LessonMaterialSpecParam { SchoolId = 1 };
            var spec = new PendingLessonMaterialSpecification(param);

            Assert.That(spec.Includes.Count, Is.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(spec.Includes[0].ToString(), Does.Contain("CreatedByUser"));
                Assert.That(spec.Includes[1].ToString(), Does.Contain("FolderLessonMaterials"));
            });
        }

        [TestCase("title", "desc")]
        [TestCase("title", "asc")]
        [TestCase("createdat", "desc")]
        [TestCase("createdat", "asc")]
        [TestCase("lastmodifiedat", "desc")]
        [TestCase("lastmodifiedat", "asc")]
        [TestCase("createdby", "desc")]
        [TestCase("createdby", "asc")]
        [TestCase("unknown", "desc")]
        [TestCase("unknown", "asc")]
        public void Should_Set_OrderBy_Correctly(string sortBy, string sortDirection)
        {
            var param = new LessonMaterialSpecParam
            {
                SortBy = sortBy,
                SortDirection = sortDirection,
                SchoolId = 1
            };
            var spec = new PendingLessonMaterialSpecification(param);

            var queryable = new[]
            {
                new LessonMaterial { Title = "B", CreatedAt = DateTimeOffset.UtcNow.AddDays(-1), LastModifiedAt = DateTimeOffset.UtcNow.AddDays(-2), CreatedByUser = new ApplicationUser { FullName = "UserB" } },
                new LessonMaterial { Title = "A", CreatedAt = DateTimeOffset.UtcNow, LastModifiedAt = DateTimeOffset.UtcNow, CreatedByUser = new ApplicationUser { FullName = "UserA" } }
            }.AsQueryable();

            var ordered = spec.OrderBy!(queryable).ToList();

            Assert.That(ordered, Is.Not.Null);
            Assert.That(ordered, Has.Count.EqualTo(2));
        }

        [Test]
        public void Should_Default_OrderBy_CreatedAt_Desc_When_SortBy_Is_Null()
        {
            var param = new LessonMaterialSpecParam { SchoolId = 1 };
            var spec = new PendingLessonMaterialSpecification(param);

            var queryable = new[]
            {
                new LessonMaterial { CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) },
                new LessonMaterial { CreatedAt = DateTimeOffset.UtcNow }
            }.AsQueryable();

            var ordered = spec.OrderBy!(queryable).ToList();
            Assert.That(ordered[0].CreatedAt, Is.GreaterThan(ordered[1].CreatedAt));
        }

        [TestCase(true, 180, 20, 10, 20)]
        [TestCase(false, 0, 1, 1, 1)]
        public void Should_Set_Paging_Correctly(bool isPagingEnabled, int expectedSkip, int expectedTake, int pageIndex, int pageSize)
        {
            var param = new LessonMaterialSpecParam
            {
                IsPagingEnabled = isPagingEnabled,
                PageIndex = pageIndex,
                PageSize = pageSize,
                SchoolId = 1
            };
            var spec = new PendingLessonMaterialSpecification(param);

            Assert.Multiple(() =>
            {
                Assert.That(spec.Skip, Is.EqualTo(expectedSkip));
                Assert.That(spec.Take, Is.EqualTo(expectedTake));
            });
        }
    }
}