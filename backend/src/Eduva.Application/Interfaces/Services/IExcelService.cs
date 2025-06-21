using Microsoft.AspNetCore.Http;

namespace Eduva.Application.Interfaces.Services
{
    public interface IExcelService
    {
        Task<byte[]> ExportImportErrorsAsync(IFormFile originalFile, Dictionary<int, string> rowErrors);
    }
}