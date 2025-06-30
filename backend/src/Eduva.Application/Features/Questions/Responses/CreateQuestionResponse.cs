namespace Eduva.Application.Features.Questions.Responses
{
    public class CreateQuestionResponse
    {
        public Guid Id { get; set; }
        public Guid LessonMaterialId { get; set; }
        public string Title { get; set; } = default!;
        public string Content { get; set; } = default!;
        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string? CreatedByName { get; set; }
        public string? CreatedByAvatar { get; set; }
        public string? CreatedByRole { get; set; }
        public int CommentCount { get; set; } = 0;
    }
}