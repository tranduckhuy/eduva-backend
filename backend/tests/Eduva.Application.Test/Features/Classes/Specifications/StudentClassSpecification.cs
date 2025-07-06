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

            Assert.That(filtered.Count, Is.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(filtered[0].StudentId, Is.EqualTo(param.StudentId));
                Assert.That(filtered[0].ClassId, Is.EqualTo(classId));
                Assert.That(filtered[0].Class.SchoolId, Is.EqualTo(schoolId));
            });
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

            Assert.That(filtered.Count, Is.EqualTo(2));
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

            Assert.That(filtered.Count, Is.EqualTo(1));
            Assert.That(filtered[0].Class.Status, Is.EqualTo(EntityStatus.Active));
        }

        [Test]
        public void Includes_Should_Contain_Class_Teacher_School_Student()
        {
            var param = new StudentClassSpecParam { PageIndex = 1, PageSize = 10 };
            var spec = new StudentClassSpecification(param);

            Assert.That(spec.Includes.Count, Is.EqualTo(4));
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
    }
}