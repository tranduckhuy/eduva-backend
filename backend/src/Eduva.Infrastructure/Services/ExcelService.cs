using Eduva.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace Eduva.Infrastructure.Services
{
    public class ExcelService : IExcelService
    {
        private const int MaxColumn = 4;

        public async Task<byte[]> ExportImportErrorsAsync(IFormFile originalFile, Dictionary<int, string> rowErrors)
        {
            ExcelPackage.License.SetNonCommercialPersonal("EDUVA");

            using var inputStream = new MemoryStream();
            await originalFile.CopyToAsync(inputStream);
            inputStream.Position = 0;

            using var package = new ExcelPackage(inputStream);
            var worksheet = package.Workbook.Worksheets[0];
            worksheet.View.FreezePanes(2, 1);

            foreach (var (row, message) in rowErrors)
            {
                HighlightRowWithError(worksheet, row, message);
            }

            worksheet.Cells.AutoFitColumns();

            using var outputStream = new MemoryStream();
            await package.SaveAsAsync(outputStream);
            return outputStream.ToArray();
        }

        private static void HighlightRowWithError(ExcelWorksheet worksheet, int row, string errorMessage)
        {
            for (int col = 1; col <= MaxColumn; col++)
            {
                var cell = worksheet.Cells[row, col];
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.LightCoral);

                if (col == 1 && cell.Comment == null)
                {
                    var comment = cell.AddComment(errorMessage, "System");
                    comment.AutoFit = true;
                }
            }
        }
    }
}