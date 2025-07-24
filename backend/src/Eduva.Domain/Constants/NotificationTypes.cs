namespace Eduva.Domain.Constants
{
    public static class NotificationTypes
    {
        #region Question Notifications

        public const string QuestionCreated = "QuestionCreated";
        public const string QuestionUpdated = "QuestionUpdated";
        public const string QuestionDeleted = "QuestionDeleted";

        #endregion

        #region Question Comment Notifications

        public const string QuestionCommented = "QuestionCommented";
        public const string QuestionCommentUpdated = "QuestionCommentUpdated";
        public const string QuestionCommentDeleted = "QuestionCommentDeleted";

        #endregion

        #region Lesson Material Notifications

        public const string LessonMaterialApproved = "LessonMaterialApproved";
        public const string LessonMaterialRejected = "LessonMaterialRejected";

        #endregion

    }
}