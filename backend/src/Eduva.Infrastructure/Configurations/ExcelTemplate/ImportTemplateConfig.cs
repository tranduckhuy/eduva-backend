using Eduva.Domain.Enums;

namespace Eduva.Infrastructure.Configurations.ExcelTemplate
{
    public class ImportTemplateConfig
    {
        public string UrlTemplateImportUser { get; set; } = string.Empty;
        public string UrlTemplateImportSchool { get; set; } = string.Empty;
        public string UrlTemplateImportLessonMaterial { get; set; } = string.Empty;

        public string? GetUrl(ImportTemplateType type)
        {
            return type switch
            {
                ImportTemplateType.User => UrlTemplateImportUser,
                _ => null
            };
        }
    }
}