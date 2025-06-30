namespace Eduva.Shared.Enums
{
    public enum CustomCode
    {
        // Success
        Success = 2000,
        Created = 2001,
        Updated = 2002,
        Deleted = 2003,
        NoContent = 2004,
        ConfirmationEmailSent = 2005,
        ResetPasswordEmailSent = 2006,
        PasswordResetSuccessful = 2007,
        RequiresOtpVerification = 2008,
        OtpSentSuccessfully = 2009,

        // Error
        ModelInvalid = 4000,
        Unauthorized = 4001,
        Forbidden = 4003,
        SystemError = 5000,

        InvalidToken = 4004,
        EmailAlreadyExists = 4005,
        ProvidedInformationIsInValid = 4006,
        UserNotExists = 4007,
        UserNotConfirmed = 4008,
        InvalidCredentials = 4009,
        UserAccountLocked = 4010,
        ConfirmEmailTokenInvalidOrExpired = 4011,
        UserAlreadyConfirmed = 4012,
        NewPasswordSameAsOld = 4013,
        OtpInvalidOrExpired = 4014,
        TwoFactorIsAlreadyEnabled = 4015,
        TwoFactorIsAlreadyDisabled = 4016,
        UserIdNotFound = 4017,
        AccessTokenInvalidOrExpired = 4018,
        UserAlreadyHasSchool = 4019,
        SchoolNotFound = 4020,
        PlanNotFound = 4021,
        PlanNotActive = 4022,
        SchoolSubscriptionNotFound = 4023,
        PaymentFailed = 4024,
        PaymentAlreadyConfirmed = 4025,
        DowngradeNotAllowed = 4026,
        SchoolSubscriptionAlreadyExists = 4027,
        SchoolAlreadyArchived = 4035,
        SubscriptionPlanMustBeArchived = 4036,
        PlanInUse = 4037,
        SchoolAlreadyActive = 4038,
        PlanAlreadyActive = 4039,
        PlanAlreadyArchived = 4040,

        InvalidRestrictedRole = 4044,
        UserNotPartOfSchool = 4045,
        FileIsRequired = 4046,
        InvalidFileType = 4047,

        // Student Class Errors
        UserNotStudent = 4048,
        ClassNotActive = 4049,
        StudentAlreadyEnrolled = 4050,
        StudentCannotEnrollDifferentSchool = 4051,
        EnrollmentFailed = 4052,

        UserAlreadyLocked = 4053,
        UserNotLocked = 4054,
        CannotLockSelf = 4055,
        CannotUnlockSelf = 4056,
        InvalidTemplateType = 4057,
        FileDownloadFailed = 4058,
        IncorrectCurrentPassword = 4059,
        OtpResendTooSoon = 4060,
        UserNotFound = 4061,
        CannotDeleteYourOwnAccount = 4062,
        UserAlreadyDeleted = 4063,
        UserMustBeLockedBeforeDelete = 4064,
        CannotUnlockDeletedUser = 4065,

        // File Storage Errors
        InvalidBlobName = 4200,
        InvalidBlobUrl = 4201,
        BlobNotFound = 4202,

        // Payment Transaction Errors
        PaymentTransactionNotFound = 4300,
        AICreditPackNotFound = 4302,
        AICreditPackAlreadyArchived = 4303,
        AICreditPackAlreadyActive = 4304,
        AICreditPackMustBeArchived = 4305,
        AICreditPackNotActive = 4306,
        InvalidPaymentPurpose = 4307,
        CreditTransactionNotFound = 4308,

        // Subscription Errors
        SchoolAndSubscriptionRequired = 4400,
        SubscriptionExpiredWithDataLossRisk = 4401,

        // Forbidden Errors
        SchoolInactive = 4500,
        SubscriptionInvalid = 4501,
        ExceedUserLimit = 4502,

        // Folder Errors
        FolderNotFound = 4600,
        FolderNameAlreadyExists = 4601,
        FolderCreateFailed = 4602,
        FolderUpdateFailed = 4603,
        FolderDeleteFailed = 4604,

        // Class Errors - All consolidated in 4700 range
        ClassNotFound = 4700,
        ClassNameAlreadyExists = 4701,
        ClassCodeDuplicate = 4702,
        ClassCreateFailed = 4703,
        ClassUpdateFailed = 4704,
        ClassArchiveFailed = 4705,
        ClassCodeResetFailed = 4706,
        ClassAlreadyArchived = 4707,
        ClassNotArchived = 4708,
        ClassMustBeArchivedBeforeDelete = 4709,
        CannotCreateClassForInactiveSchool = 4710,
        ClassRestoreFailed = 4711,
        ClassDeleteFailed = 4712,
        ClassNameAlreadyExistsForTeacher = 4713,
        NotTeacherOfClass = 4714,
        NotAdminForClassList = 4715,
        StudentNotEnrolled = 4716,
        StudentRemovalFailed = 4717,
        KeyConfigNotFound = 5001,
        KeyConfigAlreadyExist = 5002,

        // Question Errors
        InsufficientPermissionToCreateQuestion = 4800,
        LessonMaterialNotFound = 4801,
        LessonMaterialNotActive = 4802,
        InsufficientPermission = 4803,
        CannotCreateQuestionForLessonNotInYourSchool = 4804,
        CannotCreateQuestionForPendingLesson = 4805,
        StudentNotEnrolledInAnyClass = 4806,
        CannotCreateQuestionForLessonNotAccessible = 4807,
    }
}