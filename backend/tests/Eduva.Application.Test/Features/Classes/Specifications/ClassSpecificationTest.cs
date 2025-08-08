using Eduva.Application.Features.Classes.Specifications;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;

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
        public void Criteria_Should_Not_Filter_When_All_Parameters_Are_Null_Or_Empty()
        {
            var param = new ClassSpecParam
            {
                SchoolId = null,
                TeacherId = null,
                ClassId = null,
                Status = null,
                SearchTerm = null,
                PageIndex = 1,
                PageSize = 10
            };

            var spec = new ClassSpecification(param);

            var data = new List<Classroom>
            {
                new() { Id = Guid.NewGuid(), SchoolId = 1, TeacherId = Guid.NewGuid(), Name = "Math", Status = EntityStatus.Active },
                new() { Id = Guid.NewGuid(), SchoolId = 2, TeacherId = Guid.NewGuid(), Name = "History", Status = EntityStatus.Inactive }
            }.AsQueryable();

            var filtered = data.Where(spec.Criteria.Compile()).ToList();
            Assert.That(filtered, Has.Count.EqualTo(2));
        }

        [Test]
        public void Criteria_Should_Filter_By_SchoolId_Only()
        {
            var param = new ClassSpecParam
            {
                SchoolId = 1,
                PageIndex = 1,
                PageSize = 10
            };

            var spec = new ClassSpecification(param);

            var data = new List<Classroom>
            {
                new() { SchoolId = 1, Name = "Math" },
                new() { SchoolId = 2, Name = "History" }
            }.AsQueryable();

            var filtered = data.Where(spec.Criteria.Compile()).ToList();
            Assert.That(filtered, Has.Count.EqualTo(1));
            Assert.That(filtered[0].SchoolId, Is.EqualTo(1));
        }

        [Test]
        public void Criteria_Should_Filter_By_TeacherId_Only()
        {
            var teacherId = Guid.NewGuid();
            var param = new ClassSpecParam
            {
                TeacherId = teacherId,
                PageIndex = 1,
                PageSize = 10
            };

            var spec = new ClassSpecification(param);

            var data = new List<Classroom>
            {
                new() { TeacherId = teacherId, Name = "Math" },
                new() { TeacherId = Guid.NewGuid(), Name = "History" }
            }.AsQueryable();

            var filtered = data.Where(spec.Criteria.Compile()).ToList();
            Assert.That(filtered, Has.Count.EqualTo(1));
            Assert.That(filtered[0].TeacherId, Is.EqualTo(teacherId));
        }

        [Test]
        public void Criteria_Should_Filter_By_ClassId_Only()
        {
            var classId = Guid.NewGuid();
            var param = new ClassSpecParam
            {
                ClassId = classId,
                PageIndex = 1,
                PageSize = 10
            };

            var spec = new ClassSpecification(param);

            var data = new List<Classroom>
            {
                new() { Id = classId, Name = "Math" },
                new() { Id = Guid.NewGuid(), Name = "History" }
            }.AsQueryable();

            var filtered = data.Where(spec.Criteria.Compile()).ToList();
            Assert.That(filtered, Has.Count.EqualTo(1));
            Assert.That(filtered[0].Id, Is.EqualTo(classId));
        }

        [Test]
        public void Criteria_Should_Filter_By_Status_Only()
        {
            var param = new ClassSpecParam
            {
                Status = EntityStatus.Active,
                PageIndex = 1,
                PageSize = 10
            };

            var spec = new ClassSpecification(param);

            var data = new List<Classroom>
            {
                new() { Status = EntityStatus.Active, Name = "Math" },
                new() { Status = EntityStatus.Inactive, Name = "History" }
            }.AsQueryable();

            var filtered = data.Where(spec.Criteria.Compile()).ToList();
            Assert.That(filtered, Has.Count.EqualTo(1));
            Assert.That(filtered[0].Status, Is.EqualTo(EntityStatus.Active));
        }

        [Test]
        public void Criteria_Should_Filter_By_SearchTerm_In_Name_Only()
        {
            var param = new ClassSpecParam
            {
                SearchTerm = "math",
                PageIndex = 1,
                PageSize = 10
            };

            var spec = new ClassSpecification(param);

            var data = new List<Classroom>
            {
                new() { Name = "Math 101", ClassCode = "HIST01" },
                new() { Name = "History", ClassCode = "HIST02" }
            }.AsQueryable();

            var filtered = data.Where(spec.Criteria.Compile()).ToList();
            Assert.That(filtered, Has.Count.EqualTo(1));
            Assert.That(filtered[0].Name, Is.EqualTo("Math 101"));
        }

        [Test]
        public void Criteria_Should_Filter_By_SearchTerm_In_ClassCode_Only()
        {
            var param = new ClassSpecParam
            {
                SearchTerm = "MATH",
                PageIndex = 1,
                PageSize = 10
            };

            var spec = new ClassSpecification(param);

            var data = new List<Classroom>
            {
                new() { Name = "History", ClassCode = "MATH01" },
                new() { Name = "Science", ClassCode = "SCI01" }
            }.AsQueryable();

            var filtered = data.Where(spec.Criteria.Compile()).ToList();
            Assert.That(filtered, Has.Count.EqualTo(1));
            Assert.That(filtered[0].ClassCode, Is.EqualTo("MATH01"));
        }

        [Test]
        public void Criteria_Should_Handle_Null_ClassCode_In_SearchTerm()
        {
            var param = new ClassSpecParam
            {
                SearchTerm = "math",
                PageIndex = 1,
                PageSize = 10
            };

            var spec = new ClassSpecification(param);

            var data = new List<Classroom>
            {
                new() { Name = "Math 101", ClassCode = null },
                new() { Name = "History", ClassCode = "HIST01" }
            }.AsQueryable();

            var filtered = data.Where(spec.Criteria.Compile()).ToList();
            Assert.That(filtered, Has.Count.EqualTo(1));
            Assert.That(filtered[0].Name, Is.EqualTo("Math 101"));
        }

        [Test]
        public void Criteria_Should_Not_Filter_When_SearchTerm_Is_Empty()
        {
            var param = new ClassSpecParam
            {
                SearchTerm = "",
                PageIndex = 1,
                PageSize = 10
            };

            var spec = new ClassSpecification(param);

            var data = new List<Classroom>
            {
                new() { Name = "Math" },
                new() { Name = "History" }
            }.AsQueryable();

            var filtered = data.Where(spec.Criteria.Compile()).ToList();
            Assert.That(filtered, Has.Count.EqualTo(2));
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
        public void OrderBy_Should_Be_Null_When_SortBy_Is_Null_Or_Empty()
        {
            var paramNull = new ClassSpecParam { SortBy = null, PageIndex = 1, PageSize = 10 };
            var paramEmpty = new ClassSpecParam { SortBy = "", PageIndex = 1, PageSize = 10 };

            var specNull = new ClassSpecification(paramNull);
            var specEmpty = new ClassSpecification(paramEmpty);

            Assert.Multiple(() =>
            {
                Assert.That(specNull.OrderBy, Is.Null);
                Assert.That(specEmpty.OrderBy, Is.Null);
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
        public void OrderBy_Should_Order_By_LastModifiedAt_Asc_Or_Desc()
        {
            var now = DateTime.UtcNow;
            var paramAsc = new ClassSpecParam { SortBy = "lastModifiedAt", SortDirection = "asc", PageIndex = 1, PageSize = 10 };
            var paramDesc = new ClassSpecParam { SortBy = "lastModifiedAt", SortDirection = "desc", PageIndex = 1, PageSize = 10 };

            var specAsc = new ClassSpecification(paramAsc);
            var specDesc = new ClassSpecification(paramDesc);

            var data = new List<Classroom>
            {
                new() { Name = "B", LastModifiedAt = now },
                new() { Name = "A", LastModifiedAt = now.AddDays(-1) },
                new() { Name = "C", LastModifiedAt = now.AddDays(1) }
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
        public void OrderBy_Should_Order_By_CreatedAt_Asc_Or_Desc()
        {
            var now = DateTime.UtcNow;
            var paramAsc = new ClassSpecParam { SortBy = "createdAt", SortDirection = "asc", PageIndex = 1, PageSize = 10 };
            var paramDesc = new ClassSpecParam { SortBy = "createdAt", SortDirection = "desc", PageIndex = 1, PageSize = 10 };

            var specAsc = new ClassSpecification(paramAsc);
            var specDesc = new ClassSpecification(paramDesc);

            var data = new List<Classroom>
            {
                new() { Name = "B", CreatedAt = now },
                new() { Name = "A", CreatedAt = now.AddDays(-1) },
                new() { Name = "C", CreatedAt = now.AddDays(1) }
            }.AsQueryable();

            var orderedAsc = specAsc.OrderBy!(data).ToList();
            var orderedDesc = specDesc.OrderBy!(data).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(orderedAsc[0].Name, Is.EqualTo("A")); // Oldest first
                Assert.That(orderedDesc[0].Name, Is.EqualTo("C")); // Newest first
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
        public void OrderBy_Should_Default_To_Ascending_When_SortDirection_Is_Null()
        {
            var param = new ClassSpecParam { SortBy = "name", SortDirection = null!, PageIndex = 1, PageSize = 10 };
            var spec = new ClassSpecification(param);

            var data = new List<Classroom>
            {
                new() { Name = "B" },
                new() { Name = "A" },
                new() { Name = "C" }
            }.AsQueryable();

            var ordered = spec.OrderBy!(data).ToList();
            Assert.Multiple(() =>
            {
                Assert.That(ordered[0].Name, Is.EqualTo("A"));
                Assert.That(ordered[2].Name, Is.EqualTo("C"));
            });
        }

        [TestCase("NAME")]
        [TestCase("Name")]
        [TestCase("name")]
        [TestCase("LASTMODIFIEDAT")]
        [TestCase("LastModifiedAt")]
        [TestCase("lastmodifiedat")]
        [TestCase("CREATEDAT")]
        [TestCase("CreatedAt")]
        [TestCase("createdat")]
        public void OrderBy_Should_Be_Case_Insensitive(string sortBy)
        {
            var param = new ClassSpecParam { SortBy = sortBy, SortDirection = "asc", PageIndex = 1, PageSize = 10 };
            var spec = new ClassSpecification(param);

            Assert.That(spec.OrderBy, Is.Not.Null);
        }

        [TestCase("DESC")]
        [TestCase("Desc")]
        [TestCase("desc")]
        public void OrderBy_Should_Be_Descending_For_Various_Desc_Cases(string sortDirection)
        {
            var param = new ClassSpecParam { SortBy = "name", SortDirection = sortDirection, PageIndex = 1, PageSize = 10 };
            var spec = new ClassSpecification(param);

            var data = new List<Classroom>
            {
                new() { Name = "A" },
                new() { Name = "B" }
            }.AsQueryable();

            var ordered = spec.OrderBy!(data).ToList();
            Assert.That(ordered[0].Name, Is.EqualTo("B"));
        }

        [Test]
        public void OrderBy_Should_Order_By_Name_Asc_When_SortDirection_Is_Not_Desc()
        {
            var param = new ClassSpecParam { SortBy = "name", SortDirection = "ascending", PageIndex = 1, PageSize = 10 };
            var spec = new ClassSpecification(param);

            var data = new List<Classroom>
            {
                new() { Name = "B" },
                new() { Name = "A" },
                new() { Name = "C" }
            }.AsQueryable();

            var ordered = spec.OrderBy!(data).ToList();
            Assert.Multiple(() =>
            {
                Assert.That(ordered[0].Name, Is.EqualTo("A"));
                Assert.That(ordered[2].Name, Is.EqualTo("C"));
            });
        }

        [Test]
        public void OrderBy_Should_Order_By_CreatedAt_Asc_When_SortDirection_Is_Not_Desc()
        {
            var param = new ClassSpecParam { SortBy = "other", SortDirection = "ascending", PageIndex = 1, PageSize = 10 };
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
                Assert.That(ordered[0].Name, Is.EqualTo("C"));
                Assert.That(ordered[2].Name, Is.EqualTo("B"));
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

        [Test]
        public void Skip_And_Take_Should_Handle_Page_1()
        {
            var param = new ClassSpecParam { PageIndex = 1, PageSize = 10 };
            var spec = new ClassSpecification(param);

            Assert.Multiple(() =>
            {
                Assert.That(spec.Skip, Is.EqualTo(0));
                Assert.That(spec.Take, Is.EqualTo(10));
            });
        }

        [Test]
        public void Skip_And_Take_Should_Handle_Page_0()
        {
            var param = new ClassSpecParam { PageIndex = 0, PageSize = 10 };
            var spec = new ClassSpecification(param);

            Assert.Multiple(() =>
            {
                Assert.That(spec.Skip, Is.EqualTo(-10)); // (0-1) * 10 = -10
                Assert.That(spec.Take, Is.EqualTo(10));
            });
        }

        [Test]
        public void Selector_Should_Be_Null()
        {
            var param = new ClassSpecParam { PageIndex = 1, PageSize = 10 };
            var spec = new ClassSpecification(param);

            Assert.That(spec.Selector, Is.Null);
        }

        [Test]
        public void Criteria_Should_Filter_By_Multiple_Conditions_Combined()
        {
            var schoolId = 1;
            var teacherId = Guid.NewGuid();
            var classId = Guid.NewGuid();

            var param = new ClassSpecParam
            {
                SchoolId = schoolId,
                TeacherId = teacherId,
                ClassId = classId,
                Status = EntityStatus.Active,
                SearchTerm = "math",
                PageIndex = 1,
                PageSize = 10
            };

            var spec = new ClassSpecification(param);

            var data = new List<Classroom>
            {
                new() { Id = classId, SchoolId = schoolId, TeacherId = teacherId, Status = EntityStatus.Active, Name = "Math 101" },
                new() { Id = classId, SchoolId = 2, TeacherId = teacherId, Status = EntityStatus.Active, Name = "Math 101" },
                new() { Id = classId, SchoolId = schoolId, TeacherId = Guid.NewGuid(), Status = EntityStatus.Active, Name = "Math 101" },
                new() { Id = classId, SchoolId = schoolId, TeacherId = teacherId, Status = EntityStatus.Inactive, Name = "Math 101" },
                new() { Id = classId, SchoolId = schoolId, TeacherId = teacherId, Status = EntityStatus.Active, Name = "History" }
            }.AsQueryable();

            var filtered = data.Where(spec.Criteria.Compile()).ToList();
            Assert.That(filtered, Has.Count.EqualTo(1));
        }
    }
}