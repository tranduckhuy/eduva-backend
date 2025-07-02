using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Questions.Responses
{
    public class QuestionResponse
    {
        [JsonPropertyOrder(1)]
        public Guid Id { get; set; }

        [JsonPropertyOrder(2)]
        public Guid LessonMaterialId { get; set; }

        [JsonPropertyOrder(3)]
        public string? LessonMaterialTitle { get; set; }

        [JsonPropertyOrder(4)]
        public string Title { get; set; } = default!;

        [JsonPropertyOrder(5)]
        public string Content { get; set; } = default!;

        [JsonPropertyOrder(6)]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyOrder(7)]
        public DateTimeOffset? LastModifiedAt { get; set; }

        [JsonPropertyOrder(8)]
        public Guid CreatedByUserId { get; set; }

        [JsonPropertyOrder(9)]
        public string? CreatedByName { get; set; }

        [JsonPropertyOrder(10)]
        public string? CreatedByAvatar { get; set; }

        [JsonPropertyOrder(11)]
        public string? CreatedByRole { get; set; }

        [JsonPropertyOrder(12)]
        public int CommentCount { get; set; } = 0;
    }
}