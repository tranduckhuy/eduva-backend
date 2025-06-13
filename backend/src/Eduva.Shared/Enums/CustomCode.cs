namespace Eduva.Shared.Enums
{
    public enum CustomCode
    {
        // Success
        Success = 2000,

        // Auth errors
        Unauthorized = 4001,
        Forbidden = 4003,

        InvalidToken = 4004,
        EmailAlreadyExists = 4005,

        // User errors
        EmailInvalid = 40001,
        UserNotFound = 40002,
    }
}
