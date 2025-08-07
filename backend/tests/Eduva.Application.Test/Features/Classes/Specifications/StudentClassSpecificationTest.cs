using Eduva.Application.Features.Classes.Specifications;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;

namespace Eduva.Application.Test.Features.Classes.Specifications
{
    [TestFixture]
    public class StudentClassSpecificationTest
    {
        [Test]
        public void Criteria_Should_Filter_By_StudentId_And_ClassId_And_SchoolId()
        {
            var classId = Guid.NewGuid();
            var schoolId = 1;

            var param = new StudentClassSpecParam
            {
                StudentId = Guid.NewGuid(),
                SchoolId = schoolId,
                PageIndex = 1,
                PageSize = 10
            };

            var spec = new StudentClassSpecification(param, classId);

            var data = new List<StudentClass>
            {
                new() {
                    StudentId = param.StudentId,
                    ClassId = classId,
                    Class = new Classroom { SchoolId = schoolId }
                },
                new() {
                    StudentId = Guid.NewGuid(),
                    ClassId = classId,
                    Class = new Classroom { SchoolId = schoolId }
                },
                new() {
                    StudentId = param.StudentId,
                    ClassId = Guid.NewGuid(),
                    Class = new Classroom { SchoolId = schoolId }
                },
                new() {
                    StudentId = param.StudentId,
                    ClassId = classId,
                    Class = new Classroom { SchoolId = 2 }
                }
            }.AsQueryable();

            var filtered = data.Where(spec.Criteria.Compile()).ToList();

            Assert.That(filtered, Has.Count.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(filtered[0].StudentId, Is.EqualTo(param.StudentId));
                Assert.That(filtered[0].ClassId, Is.EqualTo(classId));
                Assert.That(filtered[0].Class.SchoolId, Is.EqualTo(schoolId));
            });
        }

        [Test]
        public void Constructor_Should_Work_Without_ClassId()
        {
            var param = new StudentClassSpecParam
            {
                StudentId = Guid.NewGuid(),
                PageIndex = 1,
                PageSize = 10
            };

            var spec = new StudentClassSpecification(param);
            Assert.Multiple(() =>
            {
                Assert.That(spec.Criteria, Is.Not.Null);
                Assert.That(spec.OrderBy, Is.Not.Null);
            });
        }

        [Test]
        public void Criteria_Should_Not_Filter_By_StudentId_When_Empty()
        {
            var param = new StudentClassSpecParam
            {
                StudentId = Guid.Empty, // Empty GUID
                PageIndex = 1,
                PageSize = 10
            };

            var spec = new StudentClassSpecification(param);

            var data = new List<StudentClass>
            {
                new() { StudentId = Guid.NewGuid(), Class = new Classroom() },
                new() { StudentId = Guid.NewGuid(), Class = new Classroom() }
            }.AsQueryable();

            var filtered = data.Where(spec.Criteria.Compile()).ToList();
            Assert.That(filtered, Has.Count.EqualTo(2));
        }

        [Test]
        public void Criteria_Should_Not_Filter_By_SchoolId_When_Null()
        {
            var param = new StudentClassSpecParam
            {
                SchoolId = null,
                PageIndex = 1,
                PageSize = 10
            };

            var spec = new StudentClassSpecification(param);

            var data = new List<StudentClass>
            {
                new() { Class = new Classroom { School = null! } },
                new() { Class = new Classroom { School = new School { Name = "School A" } } }
            }.AsQueryable();

            var filtered = data.Where(spec.Criteria.Compile()).ToList();
            Assert.That(filtered, Has.Count.EqualTo(2));
        }

        [Test]
        public void Criteria_Should_Filter_By_SearchTerm()
        {
            var param = new StudentClassSpecParam
            {
                SearchTerm = "math",
                PageIndex = 1,
                PageSize = 10
            };

            var spec = new StudentClassSpecification(param);

            var data = new List<StudentClass>
            {
                new() {
                    Class = new Classroom { Name = "Math 101", ClassCode = "MATH01", Teacher = new ApplicationUser { FullName = "Mr. Smith" }, School = new School { Name = "School A" } },
                    Student = new ApplicationUser { FullName = "Student One" }
                },
                new() {
                    Class = new Classroom { Name = "History", ClassCode = "HIST01", Teacher = new ApplicationUser { FullName = "Ms. Jane" }, School = new School { Name = "School B" } },
                    Student = new ApplicationUser { FullName = "Mathilda" }
                }
            }.AsQueryable();

            var filtered = data.Where(spec.Criteria.Compile()).ToList();
            Assert.That(filtered, Has.Count.EqualTo(2));
        }

        [Test]
        public void Criteria_Should_Not_Filter_When_SearchTerm_Is_Null_Or_Empty()
        {
            var param = new StudentClassSpecParam
            {
                SearchTerm = null,
                PageIndex = 1,
                PageSize = 10
            };

            var spec = new StudentClassSpecification(param);

            var data = new List<StudentClass>
            {
                new() { Class = new Classroom { Name = "Math" } },
                new() { Class = new Classroom { Name = "History" } }
            }.AsQueryable();

            var filtered = data.Where(spec.Criteria.Compile()).ToList();
            Assert.That(filtered, Has.Count.EqualTo(2));

            // Test empty string
            param.SearchTerm = "";
            spec = new StudentClassSpecification(param);
            filtered = data.Where(spec.Criteria.Compile()).ToList();
            Assert.That(filtered, Has.Count.EqualTo(2));
        }

        [Test]
        public void Criteria_Should_Filter_By_ClassName_TeacherName_SchoolName_ClassCode_And_Status()
        {
            var param = new StudentClassSpecParam
            {
                ClassName = "math",
                TeacherName = "smith",
                SchoolName = "school a",
                ClassCode = "MATH01",
                ClassStatus = EntityStatus.Active,
                PageIndex = 1,
                PageSize = 10
            };

            var spec = new StudentClassSpecification(param);

            var data = new List<StudentClass>
            {
                new() {
                    Class = new Classroom
                    {
                        Name = "Math 101",
                        ClassCode = "MATH01",
                        Status = EntityStatus.Active,
                        Teacher = new ApplicationUser { FullName = "John Smith" },
                        School = new School { Name = "School A" }
                    },
                    Student = new ApplicationUser { FullName = "Student One" }
                },
                new() {
                    Class = new Classroom
                    {
                        Name = "Math 101",
                        ClassCode = "MATH01",
                        Status = EntityStatus.Inactive,
                        Teacher = new ApplicationUser { FullName = "John Smith" },
                        School = new School { Name = "School A" }
                    },
                    Student = new ApplicationUser { FullName = "Student Two" }
                }
            }.AsQueryable();

            var filtered = data.Where(spec.Criteria.Compile()).ToList();
            Assert.That(filtered, Has.Count.EqualTo(1));
            Assert.That(filtered[0].Class.Status, Is.EqualTo(EntityStatus.Active));
        }

        [Test]
        public void Criteria_Should_Not_Filter_When_Individual_Filters_Are_Null_Or_Empty()
        {
            var param = new StudentClassSpecParam
            {
                ClassName = null,
                TeacherName = "",
                SchoolName = null,
                ClassCode = "",
                ClassStatus = null,
                PageIndex = 1,
                PageSize = 10
            };

            var spec = new StudentClassSpecification(param);

            var data = new List<StudentClass>
            {
                new() { Class = new Classroom { Name = "Math", Status = EntityStatus.Active } },
                new() { Class = new Classroom { Name = "History", Status = EntityStatus.Inactive } }
            }.AsQueryable();

            var filtered = data.Where(spec.Criteria.Compile()).ToList();
            Assert.That(filtered, Has.Count.EqualTo(2));
        }

        [Test]
        public void Criteria_Should_Handle_Null_Teacher()
        {
            var param = new StudentClassSpecParam
            {
                TeacherName = "smith",
                PageIndex = 1,
                PageSize = 10
            };

            var spec = new StudentClassSpecification(param);

            var data = new List<StudentClass>
            {
                new() { Class = new Classroom { Teacher = null! } },
                new() { Class = new Classroom { Teacher = new ApplicationUser { FullName = null } } },
                new() { Class = new Classroom { Teacher = new ApplicationUser { FullName = "John Smith" } } }
            }.AsQueryable();

            var filtered = data.Where(spec.Criteria.Compile()).ToList();
            Assert.That(filtered, Has.Count.EqualTo(1));
        }

        [Test]
        public void Criteria_Should_Handle_Null_School()
        {
            var param = new StudentClassSpecParam
            {
                SchoolName = "school a",
                PageIndex = 1,
                PageSize = 10
            };

            var spec = new StudentClassSpecification(param);

            var data = new List<StudentClass>
            {
                new() { Class = new Classroom { School = null! } },
                new() { Class = new Classroom { School = new School { Name = "School A" } } }
            }.AsQueryable();

            var filtered = data.Where(spec.Criteria.Compile()).ToList();
            Assert.That(filtered, Has.Count.EqualTo(1));
        }

        [Test]
        public void Includes_Should_Contain_Class_Teacher_School_Student()
        {
            var param = new StudentClassSpecParam { PageIndex = 1, PageSize = 10 };
            var spec = new StudentClassSpecification(param);

            Assert.That(spec.Includes, Has.Count.EqualTo(4));
            Assert.Multiple(() =>
            {
                Assert.That(spec.Includes.Any(i => i.Body.ToString().Contains("Class")), Is.True);
                Assert.That(spec.Includes.Any(i => i.Body.ToString().Contains("Teacher")), Is.True);
                Assert.That(spec.Includes.Any(i => i.Body.ToString().Contains("School")), Is.True);
                Assert.That(spec.Includes.Any(i => i.Body.ToString().Contains("Student")), Is.True);
            });
        }

        [Test]
        public void OrderBy_Should_Order_By_EnrolledAt_Desc_By_Default()
        {
            var param = new StudentClassSpecParam { PageIndex = 1, PageSize = 10 };
            var spec = new StudentClassSpecification(param);

            var now = DateTime.UtcNow;
            var data = new List<StudentClass>
            {
                new() { EnrolledAt = now.AddDays(-1) },
                new() { EnrolledAt = now },
                new() { EnrolledAt = now.AddDays(-2) }
            }.AsQueryable();

            var ordered = spec.OrderBy!(data).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(ordered[0].EnrolledAt, Is.GreaterThan(ordered[1].EnrolledAt));
                Assert.That(ordered[1].EnrolledAt, Is.GreaterThan(ordered[2].EnrolledAt));
            });
        }

        [TestCase("classname")]
        [TestCase("schoolname")]
        [TestCase("teachername")]
        [TestCase("classcode")]
        [TestCase("status")]
        [TestCase("studentname")]
        [TestCase("enrolledat")]
        [TestCase("createdat")]
        [TestCase("lastmodifiedat")]
        [TestCase("invalidfield")]
        [TestCase(null)]
        public void OrderBy_Should_Sort_By_Correct_Field(string? sortBy)
        {
            var param = new StudentClassSpecParam
            {
                SortBy = sortBy,
                SortDirection = "desc",
                PageIndex = 1,
                PageSize = 10
            };
            var spec = new StudentClassSpecification(param);

            var now = DateTime.UtcNow;
            var data = new List<StudentClass>
            {
                new()
                {
                    EnrolledAt = now.AddDays(-1),
                    Class = new Classroom
                    {
                        Name = "B",
                        School = new School { Name = "School B" },
                        Teacher = new ApplicationUser { FullName = "Teacher B" },
                        ClassCode = "B",
                        Status = EntityStatus.Inactive,
                        CreatedAt = now.AddDays(-1),
                        LastModifiedAt = now.AddDays(-1)
                    },
                    Student = new ApplicationUser { FullName = "Student B" }
                },
                new()
                {
                    EnrolledAt = now,
                    Class = new Classroom
                    {
                        Name = "A",
                        School = new School { Name = "School A" },
                        Teacher = new ApplicationUser { FullName = "Teacher A" },
                        ClassCode = "A",
                        Status = EntityStatus.Active,
                        CreatedAt = now,
                        LastModifiedAt = now
                    },
                    Student = new ApplicationUser { FullName = "Student A" }
                }
            }.AsQueryable();

            var ordered = spec.OrderBy!(data).ToList();

            switch (sortBy)
            {
                case "classname":
                    Assert.That(ordered[0].Class.Name, Is.EqualTo("B"));
                    break;
                case "schoolname":
                    Assert.That(ordered[0].Class.School.Name, Is.EqualTo("School B"));
                    break;
                case "teachername":
                    Assert.That(ordered[0].Class.Teacher.FullName, Is.EqualTo("Teacher B"));
                    break;
                case "classcode":
                    Assert.That(ordered[0].Class.ClassCode, Is.EqualTo("B"));
                    break;
                case "status":
                    Assert.That(ordered[0].Class.Status, Is.EqualTo(EntityStatus.Inactive));
                    break;
                case "studentname":
                    Assert.That(ordered[0].Student.FullName, Is.EqualTo("Student B"));
                    break;
                case "createdat":
                    Assert.That(ordered[0].Class.CreatedAt, Is.GreaterThan(ordered[1].Class.CreatedAt));
                    break;
                case "lastmodifiedat":
                    Assert.That(ordered[0].Class.LastModifiedAt, Is.GreaterThan(ordered[1].Class.LastModifiedAt));
                    break;
                default:
                    Assert.That(ordered[0].EnrolledAt, Is.GreaterThan(ordered[1].EnrolledAt));
                    break;
            }
        }

        [Test]
        public void OrderBy_Should_Handle_Null_Values_In_Sort_Fields()
        {
            var param = new StudentClassSpecParam
            {
                SortBy = "teachername",
                SortDirection = "asc",
                PageIndex = 1,
                PageSize = 10
            };
            var spec = new StudentClassSpecification(param);

            var data = new List<StudentClass>
            {
                new() { Class = new Classroom { Teacher = null! } },
                new() { Class = new Classroom { Teacher = new ApplicationUser { FullName = null } } },
                new() { Class = new Classroom { Teacher = new ApplicationUser { FullName = "Teacher A" } } }
            }.AsQueryable();

            var ordered = spec.OrderBy!(data).ToList();
            Assert.That(ordered, Has.Count.EqualTo(3));
        }

        [Test]
        public void Selector_Should_Be_Null()
        {
            var param = new StudentClassSpecParam { PageIndex = 1, PageSize = 10 };
            var spec = new StudentClassSpecification(param);
            Assert.That(spec.Selector, Is.Null);
        }

        [Test]
        public void Skip_And_Take_Should_Be_Set_Correctly()
        {
            var param = new StudentClassSpecParam { PageIndex = 2, PageSize = 5 };
            var spec = new StudentClassSpecification(param);

            Assert.Multiple(() =>
            {
                Assert.That(spec.Skip, Is.EqualTo(5));
                Assert.That(spec.Take, Is.EqualTo(5));
            });
        }

        [Test]
        public void Skip_And_Take_Should_Handle_Edge_Cases()
        {
            var param = new StudentClassSpecParam { PageIndex = 1, PageSize = 10 };
            var spec = new StudentClassSpecification(param);
            Assert.Multiple(() =>
            {
                Assert.That(spec.Skip, Is.EqualTo(0));
                Assert.That(spec.Take, Is.EqualTo(10));
            });

            param = new StudentClassSpecParam { PageIndex = 0, PageSize = 10 };
            spec = new StudentClassSpecification(param);
            Assert.That(spec.Skip, Is.EqualTo(-10));
        }

        [Test]
        public void OrderBy_Should_Order_Ascending_When_SortDirection_Is_Not_Desc()
        {
            var param = new StudentClassSpecParam
            {
                SortBy = "classname",
                SortDirection = "asc",
                PageIndex = 1,
                PageSize = 10
            };
            var spec = new StudentClassSpecification(param);

            var data = new List<StudentClass>
            {
                new() { Class = new Classroom { Name = "B" } },
                new() { Class = new Classroom { Name = "A" } },
                new() { Class = new Classroom { Name = "C" } }
            }.AsQueryable();

            var ordered = spec.OrderBy!(data).ToList();
            Assert.Multiple(() =>
            {
                Assert.That(ordered[0].Class.Name, Is.EqualTo("A"));
                Assert.That(ordered[2].Class.Name, Is.EqualTo("C"));
            });
        }

        [TestCase("ASC")]
        [TestCase("ascending")]
        [TestCase("up")]
        [TestCase(null)]
        [TestCase("")]
        public void OrderBy_Should_Default_To_Ascending_When_SortDirection_Is_Not_Desc(string? sortDirection)
        {
            var param = new StudentClassSpecParam
            {
                SortBy = "classname",
                SortDirection = sortDirection ?? string.Empty,
                PageIndex = 1,
                PageSize = 10
            };
            var spec = new StudentClassSpecification(param);

            var data = new List<StudentClass>
    {
        new() { Class = new Classroom { Name = "B" } },
        new() { Class = new Classroom { Name = "A" } }
    }.AsQueryable();

            var ordered = spec.OrderBy!(data).ToList();
            Assert.That(ordered[0].Class.Name, Is.EqualTo("A"));
        }

        [Test]
        public void OrderBy_Should_Order_By_StudentName_Asc()
        {
            var param = new StudentClassSpecParam
            {
                SortBy = "studentname",
                SortDirection = "asc",
                PageIndex = 1,
                PageSize = 10
            };
            var spec = new StudentClassSpecification(param);

            var data = new List<StudentClass>
            {
                new() { Student = new ApplicationUser { FullName = "B" } },
                new() { Student = new ApplicationUser { FullName = "A" } },
                new() { Student = new ApplicationUser { FullName = "C" } }
            }.AsQueryable();

            var ordered = spec.OrderBy!(data).ToList();
            Assert.Multiple(() =>
            {
                Assert.That(ordered[0].Student.FullName, Is.EqualTo("A"));
                Assert.That(ordered[2].Student.FullName, Is.EqualTo("C"));
            });
        }
    }
}