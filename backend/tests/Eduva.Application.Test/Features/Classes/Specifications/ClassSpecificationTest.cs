using Eduva.Application.Features.Classes.Specifications;
using Eduva.Domain.Entities;

namespace Eduva.Application.Test.Features.Classes.Specifications
{
    [TestFixture]
    public class ClassSpecificationTest
    {
        [Test]
        public void Criteria_Should_Filter_By_SchoolId_TeacherId_ClassId_And_SearchTerm()
        {
            var schoolId = 1;
            var teacherId = Guid.NewGuid();
            var classId = Guid.NewGuid();

            var param = new ClassSpecParam
            {
                SchoolId = schoolId,
                TeacherId = teacherId,
                ClassId = classId,
                SearchTerm = "math",
                PageIndex = 1,
                PageSize = 10
            };

            var spec = new ClassSpecification(param);

            var data = new List<Classroom>
            {
                new() { Id = classId, SchoolId = schoolId, TeacherId = teacherId, Name = "Math 101", ClassCode = "MATH01" },
                new() { Id = Guid.NewGuid(), SchoolId = schoolId, TeacherId = teacherId, Name = "History", ClassCode = "HIST01" },
                new() { Id = Guid.NewGuid(), SchoolId = 2, TeacherId = teacherId, Name = "Math 102", ClassCode = "MATH02" }
            }.AsQueryable();

            var filtered = data.Where(spec.Criteria.Compile()).ToList();
            Assert.That(filtered, Has.Count.EqualTo(1));
            Assert.That(filtered[0].Name, Is.EqualTo("Math 101"));

        }

        [Test]
        public void Includes_Should_Contain_Teacher_And_School()
        {
            var param = new ClassSpecParam { PageIndex = 1, PageSize = 10 };
            var spec = new ClassSpecification(param);

            Assert.That(spec.Includes, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(spec.Includes.Any(static i => i.Body.ToString().Contains("Teacher")), Is.True);
                Assert.That(spec.Includes.Any(i => i.Body.ToString().Contains("School")), Is.True);
            });
        }

        [Test]
        public void OrderBy_Should_Order_By_Name_Asc_Or_Desc()
        {
            var paramAsc = new ClassSpecParam { SortBy = "name", SortDirection = "asc", PageIndex = 1, PageSize = 10 };
            var paramDesc = new ClassSpecParam { SortBy = "name", SortDirection = "desc", PageIndex = 1, PageSize = 10 };

            var specAsc = new ClassSpecification(paramAsc);
            var specDesc = new ClassSpecification(paramDesc);

            var data = new List<Classroom>
            {
                new() { Name = "B" },
                new() { Name = "A" },
                new() { Name = "C" }
            }.AsQueryable();

            var orderedAsc = specAsc.OrderBy!(data).ToList();
            var orderedDesc = specDesc.OrderBy!(data).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(orderedAsc[0].Name, Is.EqualTo("A"));
                Assert.That(orderedDesc[0].Name, Is.EqualTo("C"));
            });
        }

        [Test]
        public void OrderBy_Should_Order_By_CreatedAt_If_Not_SortBy_Name()
        {
            var param = new ClassSpecParam { SortBy = "other", SortDirection = "desc", PageIndex = 1, PageSize = 10 };
            var spec = new ClassSpecification(param);

            var now = DateTime.UtcNow;
            var data = new List<Classroom>
            {
                new() { Name = "A", CreatedAt = now.AddDays(-1) },
                new() { Name = "B", CreatedAt = now },
                new() { Name = "C", CreatedAt = now.AddDays(-2) }
            }.AsQueryable();

            var ordered = spec.OrderBy!(data).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(ordered[0].Name, Is.EqualTo("B"));
                Assert.That(ordered[2].Name, Is.EqualTo("C"));
            });
        }

        [Test]
        public void Skip_And_Take_Should_Be_Set_Correctly()
        {
            var param = new ClassSpecParam { PageIndex = 2, PageSize = 5 };
            var spec = new ClassSpecification(param);

            Assert.Multiple(() =>
            {
                Assert.That(spec.Skip, Is.EqualTo(5));
                Assert.That(spec.Take, Is.EqualTo(5));
            });
        }
    }
}