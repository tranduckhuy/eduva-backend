using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Questions.Responses
{
    public class QuestionDetailResponse : QuestionResponse
    {
        /* public Guid Id { get; set; }
         public Guid LessonMaterialId { get; set; }
         public string? LessonMaterialTitle { get; set; }
         public string Title { get; set; } = default!;
         public string Content { get; set; } = default!;
         public DateTimeOffset CreatedAt { get; set; }
         public DateTimeOffset? LastModifiedAt { get; set; }
         public Guid CreatedByUserId { get; set; }
         public string? CreatedByName { get; set; }
         public string? CreatedByAvatar { get; set; }
         public string? CreatedByRole { get; set; }
         public int CommentCount { get; set; } = 0;*/

        // Permission flags
        [JsonPropertyOrder(13)]
        public bool CanUpdate { get; set; }
        [JsonPropertyOrder(14)]
        public bool CanDelete { get; set; }
        [JsonPropertyOrder(15)]
        public bool CanComment { get; set; }

        // Only top-level comments
        [JsonPropertyOrder(16)]
        public List<QuestionCommentResponse> Comments { get; set; } = [];
    }

    public class QuestionCommentResponse
    {
        public Guid Id { get; set; }
        public Guid QuestionId { get; set; }
        public string Content { get; set; } = default!;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastModifiedAt { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string? CreatedByName { get; set; }
        public string? CreatedByAvatar { get; set; }
        public string? CreatedByRole { get; set; }
        public bool CanUpdate { get; set; }
        public bool CanDelete { get; set; }

        public Guid? ParentCommentId { get; set; }
        public List<QuestionReplyResponse> Replies { get; set; } = [];
        public int ReplyCount { get; set; } = 0;
    }

    public class QuestionReplyResponse
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = default!;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastModifiedAt { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string? CreatedByName { get; set; }
        public string? CreatedByAvatar { get; set; }
        public string? CreatedByRole { get; set; }
        public bool CanUpdate { get; set; }
        public bool CanDelete { get; set; }
        public Guid ParentCommentId { get; set; }
    }
}