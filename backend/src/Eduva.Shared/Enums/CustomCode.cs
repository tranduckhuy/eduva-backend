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


        // File Storage Errors
        InvalidBlobName = 4200,
        InvalidBlobUrl = 4201,
        BlobNotFound = 4202,
    }
}