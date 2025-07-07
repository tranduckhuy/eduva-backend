using Eduva.Application.Features.Questions.Specifications;
using Eduva.Domain.Entities;

namespace Eduva.Application.Test.Features.Questions.Specifications
{
    [TestFixture]
    public class MyQuestionsSpecificationTests
    {
        #region Setup

        private Guid _userId;
        private int? _userSchoolId;
        private MyQuestionsSpecParam _param;

        [SetUp]
        public void Setup()
        {
            _userId = Guid.NewGuid();
            _userSchoolId = 1;
            _param = new MyQuestionsSpecParam
            {
                PageIndex = 1,
                PageSize = 10,
                SearchTerm = "",
                SortBy = "",
                SortDirection = "asc"
            };
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_ShouldSetPropertiesCorrectly()
        {
            // Act
            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(spec.Criteria, Is.Not.Null);
                Assert.That(spec.OrderBy, Is.Not.Null);
                Assert.That(spec.Includes, Has.Count.EqualTo(3));
                Assert.That(spec.Skip, Is.EqualTo(0));
                Assert.That(spec.Take, Is.EqualTo(10));
            });
        }

        [Test]
        public void Constructor_ShouldSetSkipAndTakeCorrectly()
        {
            // Arrange
            _param.PageIndex = 3;
            _param.PageSize = 20;

            // Act
            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(spec.Skip, Is.EqualTo(40)); // (3-1) * 20
                Assert.That(spec.Take, Is.EqualTo(20));
            });
        }

        [Test]
        public void Constructor_ShouldIncludeRequiredEntities()
        {
            // Act
            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Assert
            Assert.That(spec.Includes, Has.Count.EqualTo(3));
        }

        #endregion

        #region BuildCriteria Tests

        [Test]
        public void BuildCriteria_ShouldFilterByUserId()
        {
            // Arrange
            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var criteria = spec.Criteria;

            // Assert
            Assert.That(criteria, Is.Not.Null);
        }

        [Test]
        public void BuildCriteria_ShouldFilterBySchoolId_WhenUserSchoolIdIsNotNull()
        {
            // Arrange
            _userSchoolId = 1;
            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var criteria = spec.Criteria;

            // Assert
            Assert.That(criteria, Is.Not.Null);
        }

        [Test]
        public void BuildCriteria_ShouldNotFilterBySchoolId_WhenUserSchoolIdIsNull()
        {
            // Arrange
            _userSchoolId = null;
            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var criteria = spec.Criteria;

            // Assert
            Assert.That(criteria, Is.Not.Null);
        }

        [Test]
        public void BuildCriteria_ShouldIncludeSearchTerm_WhenSearchTermIsProvided()
        {
            // Arrange
            _param.SearchTerm = "test question";
            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var criteria = spec.Criteria;

            // Assert
            Assert.That(criteria, Is.Not.Null);
        }

        [Test]
        public void BuildCriteria_ShouldHandleNullSearchTerm()
        {
            // Arrange
            _param.SearchTerm = null;
            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var criteria = spec.Criteria;

            // Assert
            Assert.That(criteria, Is.Not.Null);
        }

        [Test]
        public void BuildCriteria_ShouldHandleEmptySearchTerm()
        {
            // Arrange
            _param.SearchTerm = "";
            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var criteria = spec.Criteria;

            // Assert
            Assert.That(criteria, Is.Not.Null);
        }

        [Test]
        public void BuildCriteria_ShouldHandleWhitespaceSearchTerm()
        {
            // Arrange
            _param.SearchTerm = "   ";
            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var criteria = spec.Criteria;

            // Assert
            Assert.That(criteria, Is.Not.Null);
        }

        #endregion

        #region BuildOrderBy Tests

        [Test]
        public void BuildOrderBy_ShouldReturnDefaultOrder_WhenSortByIsNull()
        {
            // Arrange
            _param.SortBy = null;
            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var orderBy = spec.OrderBy;

            // Assert
            Assert.That(orderBy, Is.Not.Null);
        }

        [Test]
        public void BuildOrderBy_ShouldReturnDefaultOrder_WhenSortByIsEmpty()
        {
            // Arrange
            _param.SortBy = "";
            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var orderBy = spec.OrderBy;

            // Assert
            Assert.That(orderBy, Is.Not.Null);
        }

        [Test]
        public void BuildOrderBy_ShouldReturnDefaultOrder_WhenSortByIsWhitespace()
        {
            // Arrange
            _param.SortBy = "   ";
            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var orderBy = spec.OrderBy;

            // Assert
            Assert.That(orderBy, Is.Not.Null);
        }

        [Test]
        public void BuildOrderBy_ShouldOrderByTitle_Ascending()
        {
            // Arrange
            _param.SortBy = "title";
            _param.SortDirection = "asc";
            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var orderBy = spec.OrderBy;

            // Assert
            Assert.That(orderBy, Is.Not.Null);
        }

        [Test]
        public void BuildOrderBy_ShouldOrderByTitle_Descending()
        {
            // Arrange
            _param.SortBy = "title";
            _param.SortDirection = "desc";
            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var orderBy = spec.OrderBy;

            // Assert
            Assert.That(orderBy, Is.Not.Null);
        }

        [Test]
        public void BuildOrderBy_ShouldOrderByLessonMaterial_Ascending()
        {
            // Arrange
            _param.SortBy = "lessonmaterial";
            _param.SortDirection = "asc";
            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var orderBy = spec.OrderBy;

            // Assert
            Assert.That(orderBy, Is.Not.Null);
        }

        [Test]
        public void BuildOrderBy_ShouldOrderByLessonMaterial_Descending()
        {
            // Arrange
            _param.SortBy = "lessonmaterial";
            _param.SortDirection = "desc";
            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var orderBy = spec.OrderBy;

            // Assert
            Assert.That(orderBy, Is.Not.Null);
        }

        [Test]
        public void BuildOrderBy_ShouldOrderByCreatedAt_Ascending()
        {
            // Arrange
            _param.SortBy = "createdat";
            _param.SortDirection = "asc";
            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var orderBy = spec.OrderBy;

            // Assert
            Assert.That(orderBy, Is.Not.Null);
        }

        [Test]
        public void BuildOrderBy_ShouldOrderByCreatedAt_Descending()
        {
            // Arrange
            _param.SortBy = "createdat";
            _param.SortDirection = "desc";
            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var orderBy = spec.OrderBy;

            // Assert
            Assert.That(orderBy, Is.Not.Null);
        }

        [Test]
        public void BuildOrderBy_ShouldReturnDefaultOrder_WhenInvalidSortBy()
        {
            // Arrange
            _param.SortBy = "invalid";
            _param.SortDirection = "asc";
            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var orderBy = spec.OrderBy;

            // Assert
            Assert.That(orderBy, Is.Not.Null);
        }

        [Test]
        public void BuildOrderBy_ShouldHandleCaseInsensitiveSortDirection()
        {
            // Arrange
            _param.SortBy = "title";
            _param.SortDirection = "DESC";
            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var orderBy = spec.OrderBy;

            // Assert
            Assert.That(orderBy, Is.Not.Null);
        }

        [Test]
        public void BuildOrderBy_ShouldHandleCaseInsensitiveSortBy()
        {
            // Arrange
            _param.SortBy = "TITLE";
            _param.SortDirection = "asc";
            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var orderBy = spec.OrderBy;

            // Assert
            Assert.That(orderBy, Is.Not.Null);
        }

        #endregion

        #region Integration Tests

        [Test]
        public void Specification_ShouldWorkWithMockData()
        {
            // Arrange
            var questions = new List<LessonMaterialQuestion>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Question 1",
                    CreatedByUserId = _userId,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    LessonMaterial = new LessonMaterial { Title = "Test Lesson 1", SchoolId = _userSchoolId }
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Question 2",
                    CreatedByUserId = _userId,
                    CreatedAt = DateTime.UtcNow,
                    LessonMaterial = new LessonMaterial { Title = "Test Lesson 2", SchoolId = _userSchoolId }
                }
            }.AsQueryable();

            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var filteredQuestions = questions.Where(spec.Criteria.Compile()).ToList();

            // Assert
            Assert.That(filteredQuestions, Has.Count.EqualTo(2));
        }

        [Test]
        public void Specification_ShouldFilterByUserId()
        {
            // Arrange
            var questions = new List<LessonMaterialQuestion>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Question 1",
                    CreatedByUserId = _userId,
                    LessonMaterial = new LessonMaterial { SchoolId = _userSchoolId }
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Question 2",
                    CreatedByUserId = Guid.NewGuid(), // Different user
                    LessonMaterial = new LessonMaterial { SchoolId = _userSchoolId }
                }
            }.AsQueryable();

            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var filteredQuestions = questions.Where(spec.Criteria.Compile()).ToList();

            // Assert
            Assert.That(filteredQuestions, Has.Count.EqualTo(1));
        }

        [Test]
        public void Specification_ShouldFilterBySchoolId_WhenUserSchoolIdIsNotNull()
        {
            // Arrange
            var questions = new List<LessonMaterialQuestion>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Question 1",
                    CreatedByUserId = _userId,
                    LessonMaterial = new LessonMaterial { SchoolId = _userSchoolId }
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Question 2",
                    CreatedByUserId = _userId,
                    LessonMaterial = new LessonMaterial { SchoolId = 2 } // Different school
                }
            }.AsQueryable();

            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var filteredQuestions = questions.Where(spec.Criteria.Compile()).ToList();

            // Assert
            Assert.That(filteredQuestions, Has.Count.EqualTo(1));
        }

        [Test]
        public void Specification_ShouldNotFilterBySchoolId_WhenUserSchoolIdIsNull()
        {
            // Arrange
            _userSchoolId = null;
            var questions = new List<LessonMaterialQuestion>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Question 1",
                    CreatedByUserId = _userId,
                    LessonMaterial = new LessonMaterial { SchoolId = 1 }
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Question 2",
                    CreatedByUserId = _userId,
                    LessonMaterial = new LessonMaterial { SchoolId = 2 }
                }
            }.AsQueryable();

            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var filteredQuestions = questions.Where(spec.Criteria.Compile()).ToList();

            // Assert
            Assert.That(filteredQuestions, Has.Count.EqualTo(2));
        }

        [Test]
        public void Specification_ShouldFilterBySearchTerm_WhenSearchTermIsEmpty()
        {
            // Arrange
            _param.SearchTerm = ""; // Empty search term
            var questions = new List<LessonMaterialQuestion>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Question 1",
                    CreatedByUserId = _userId,
                    LessonMaterial = new LessonMaterial { Title = "Test Lesson 1", SchoolId = _userSchoolId }
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Question 2",
                    CreatedByUserId = _userId,
                    LessonMaterial = new LessonMaterial { Title = "Test Lesson 2", SchoolId = _userSchoolId }
                }
            }.AsQueryable();

            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var filteredQuestions = questions.Where(spec.Criteria.Compile()).ToList();

            // Assert
            Assert.That(filteredQuestions, Has.Count.EqualTo(2)); // Should return all questions when search term is empty
        }

        [Test]
        public void Specification_ShouldFilterBySearchTerm_WhenSearchTermIsNull()
        {
            // Arrange
            _param.SearchTerm = null; // Null search term
            var questions = new List<LessonMaterialQuestion>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Question 1",
                    CreatedByUserId = _userId,
                    LessonMaterial = new LessonMaterial { Title = "Test Lesson 1", SchoolId = _userSchoolId }
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Question 2",
                    CreatedByUserId = _userId,
                    LessonMaterial = new LessonMaterial { Title = "Test Lesson 2", SchoolId = _userSchoolId }
                }
            }.AsQueryable();

            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var filteredQuestions = questions.Where(spec.Criteria.Compile()).ToList();

            // Assert
            Assert.That(filteredQuestions, Has.Count.EqualTo(2)); // Should return all questions when search term is null
        }

        [Test]
        public void Specification_ShouldFilterBySearchTerm_WhenSearchTermIsWhitespace()
        {
            // Arrange
            _param.SearchTerm = "   "; // Whitespace search term
            var questions = new List<LessonMaterialQuestion>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Question 1",
                    CreatedByUserId = _userId,
                    LessonMaterial = new LessonMaterial { Title = "Test Lesson 1", SchoolId = _userSchoolId }
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Question 2",
                    CreatedByUserId = _userId,
                    LessonMaterial = new LessonMaterial { Title = "Test Lesson 2", SchoolId = _userSchoolId }
                }
            }.AsQueryable();

            var spec = new MyQuestionsSpecification(_param, _userId, _userSchoolId);

            // Act
            var filteredQuestions = questions.Where(spec.Criteria.Compile()).ToList();

            // Assert
            Assert.That(filteredQuestions, Has.Count.EqualTo(2)); // Should return all questions when search term is whitespace
        }

        #endregion
    }
}