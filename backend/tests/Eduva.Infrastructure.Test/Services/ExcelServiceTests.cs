using Eduva.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;

namespace Eduva.Infrastructure.Test.Services
{
    [TestFixture]
    public class ExcelServiceTests
    {
        private ExcelService _service = default!;
        private List<MemoryStream> _disposables = default!;

        #region ExcelServiceTests Setup and Teardown

        [SetUp]
        public void Setup()
        {
            _service = new ExcelService();
            _disposables = new List<MemoryStream>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var stream in _disposables)
            {
                stream.Dispose();
            }
        }

        #endregion

        #region ExcelService Tests

        [Test]
        public async Task ExportImportErrorsAsync_ShouldReturnModifiedFile_WithErrorHighlights()
        {
            var originalFile = CreateTestExcelFile();
            var rowErrors = new Dictionary<int, string>
        {
            { 2, "Invalid Email" },
            { 3, "Password too weak" }
        };

            var resultBytes = await _service.ExportImportErrorsAsync(originalFile, rowErrors);

            Assert.That(resultBytes, Is.Not.Null);
            Assert.That(resultBytes.Length, Is.GreaterThan(0));

            using var resultPackage = new ExcelPackage(new MemoryStream(resultBytes));
            var worksheet = resultPackage.Workbook.Worksheets[0];

            Assert.That(worksheet.Cells[2, 1].Comment, Is.Not.Null);
            Assert.That(worksheet.Cells[2, 1].Comment.Text, Does.Contain("Invalid Email"));
        }

        #endregion

        #region Helper Methods

        private IFormFile CreateTestExcelFile()
        {
            ExcelPackage.License.SetNonCommercialPersonal("EDUVA");
            var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Users");

            worksheet.Cells[1, 1].Value = "Email";
            worksheet.Cells[1, 2].Value = "FullName";
            worksheet.Cells[1, 3].Value = "Role";
            worksheet.Cells[1, 4].Value = "Password";

            worksheet.Cells[2, 1].Value = "invalid@";
            worksheet.Cells[2, 2].Value = "Name 1";
            worksheet.Cells[2, 3].Value = "Teacher";
            worksheet.Cells[2, 4].Value = "123";

            worksheet.Cells[3, 1].Value = "valid@example.com";
            worksheet.Cells[3, 2].Value = "Name 2";
            worksheet.Cells[3, 3].Value = "Student";
            worksheet.Cells[3, 4].Value = "abc";

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            _disposables.Add(stream);

            return new FormFile(stream, 0, stream.Length, "file", "test.xlsx");
        }

        #endregion

    }
}