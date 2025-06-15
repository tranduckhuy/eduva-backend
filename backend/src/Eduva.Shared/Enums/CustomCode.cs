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

        // Auth errors
        ModelInvalid = 4000,
        Unauthorized = 4001,
        Forbidden = 4003,


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
    }
}