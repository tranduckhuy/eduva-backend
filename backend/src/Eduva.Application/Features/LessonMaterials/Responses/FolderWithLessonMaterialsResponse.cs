namespace Eduva.Application.Features.LessonMaterials.Responses
{
    public class FolderWithLessonMaterialsResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public int CountLessonMaterials { get; set; }

        public List<LessonMaterialResponse> LessonMaterials { get; set; } = [];
    }
}
