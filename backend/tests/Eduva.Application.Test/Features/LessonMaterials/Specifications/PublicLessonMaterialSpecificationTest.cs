using Eduva.Application.Features.LessonMaterials.Specifications;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;

namespace Eduva.Application.Test.Features.LessonMaterials.Specifications
{
    [TestFixture]
    public class PublicLessonMaterialSpecificationTest
    {
        [Test]
        public void Should_Set_Criteria_Correctly()
        {
            var param = new LessonMaterialSpecParam
            {
                SchoolId = 1,
                CreatedByUserId = Guid.NewGuid(),
                SearchTerm = "math",
                ContentType = ContentType.Video,
                EntityStatus = EntityStatus.Active
            };

            var lm = new LessonMaterial
            {
                SchoolId = 1,
                CreatedByUserId = param.CreatedByUserId.Value,
                Title = "Math for kids",
                ContentType = ContentType.Video,
                LessonStatus = LessonMaterialStatus.Approved,
                Visibility = LessonMaterialVisibility.School,
                Status = EntityStatus.Active
            };

            var spec = new PublicLessonMaterialSpecification(param);

            Assert.That(spec.Criteria.Compile()(lm), Is.True);
        }

        [Test]
        public void Should_Set_Criteria_False_When_Not_Match()
        {
            var param = new LessonMaterialSpecParam
            {
                SchoolId = 2,
                CreatedByUserId = Guid.NewGuid(),
                SearchTerm = "science",
                ContentType = ContentType.DOCX,
                EntityStatus = EntityStatus.Deleted
            };

            var lm = new LessonMaterial
            {
                SchoolId = 1,
                CreatedByUserId = Guid.NewGuid(),
                Title = "Math for kids",
                ContentType = ContentType.Video,
                LessonStatus = LessonMaterialStatus.Pending,
                Visibility = LessonMaterialVisibility.Private,
                Status = EntityStatus.Active
            };

            var spec = new PublicLessonMaterialSpecification(param);

            Assert.That(spec.Criteria.Compile()(lm), Is.False);
        }

        [Test]
        public void Should_Include_CreatedByUser()
        {
            var param = new LessonMaterialSpecParam { SchoolId = 1 };
            var spec = new PublicLessonMaterialSpecification(param);

            Assert.That(spec.Includes, Has.Count.EqualTo(1));
            Assert.That(spec.Includes[0].ToString(), Does.Contain("CreatedByUser"));
        }

        [TestCase("title", "desc")]
        [TestCase("title", "asc")]
        [TestCase("createdat", "desc")]
        [TestCase("createdat", "asc")]
        [TestCase("unknown", "desc")]
        [TestCase("unknown", "asc")]
        public void Should_Set_OrderBy_Correctly(string sortBy, string sortDirection)
        {
            var param = new LessonMaterialSpecParam
            {
                SortBy = sortBy,
                SortDirection = sortDirection,
                SchoolId = 1,
                PageIndex = 1,
                PageSize = 10
            };
            var spec = new PublicLessonMaterialSpecification(param);

            var queryable = new[]
            {
                new LessonMaterial { Title = "B", CreatedAt = DateTimeOffset.UtcNow.AddDays(-1), LastModifiedAt = DateTimeOffset.UtcNow.AddDays(-2) },
                new LessonMaterial { Title = "A", CreatedAt = DateTimeOffset.UtcNow, LastModifiedAt = DateTimeOffset.UtcNow }
            }.AsQueryable();

            var ordered = spec.OrderBy!(queryable).ToList();

            Assert.That(ordered, Is.Not.Null);
            Assert.That(ordered, Has.Count.EqualTo(2));
        }

        [Test]
        public void Should_Set_Paging_Correctly()
        {
            var param = new LessonMaterialSpecParam
            {
                SchoolId = 1,
                PageIndex = 2,
                PageSize = 5
            };
            var spec = new PublicLessonMaterialSpecification(param);

            Assert.Multiple(() =>
            {
                Assert.That(spec.Skip, Is.EqualTo(5));
                Assert.That(spec.Take, Is.EqualTo(5));
            });
        }
    }
}