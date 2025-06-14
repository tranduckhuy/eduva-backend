using Eduva.Shared.Enums;
using Eduva.Shared.Models;
using Microsoft.AspNetCore.Http;

namespace Eduva.Shared.Constants
{
    public static class ResponseMessages
    {
        public static readonly Dictionary<CustomCode, MessageDetail> Messages = new Dictionary<CustomCode, MessageDetail>
        {
            #region Success Codes
            { CustomCode.Success, new MessageDetail {
                HttpCode = StatusCodes.Status200OK, Message = "Success" } },
            { CustomCode.Created, new MessageDetail {
                HttpCode = StatusCodes.Status201Created, Message = "Resource created successfully" } },
            { CustomCode.Updated, new MessageDetail {
                HttpCode = StatusCodes.Status200OK, Message = "Resource updated successfully" } },
            { CustomCode.Deleted, new MessageDetail {
                HttpCode = StatusCodes.Status200OK, Message = "Resource deleted successfully" } },
            { CustomCode.NoContent, new MessageDetail {
                HttpCode = StatusCodes.Status204NoContent, Message = "No content available" } },
            { CustomCode.ConfirmationEmailSent, new MessageDetail {
                HttpCode = StatusCodes.Status200OK, Message = "Confirmation email sent successfully" } },
            { CustomCode.ResetPasswordEmailSent, new MessageDetail {
                HttpCode = StatusCodes.Status200OK, Message = "Reset password email sent successfully" } },
            { CustomCode.PasswordResetSuccessful, new MessageDetail {
                HttpCode = StatusCodes.Status200OK, Message = "Password reset successful" } },

            #endregion


            #region Error Codes
            { CustomCode.ModelInvalid, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Invalid model state" } },
            { CustomCode.Unauthorized, new MessageDetail {
                HttpCode = StatusCodes.Status401Unauthorized, Message = "Unauthorized" } },
            { CustomCode.Forbidden, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "Access denied" } },
            { CustomCode.InvalidToken, new MessageDetail {
                HttpCode = StatusCodes.Status401Unauthorized, Message = "Invalid token. Please log in again" } },
            { CustomCode.EmailAlreadyExists, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Email already exists" } },
            { CustomCode.ProvidedInformationIsInValid, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Provided information is invalid" } },
            { CustomCode.UserNotExists, new MessageDetail {
                HttpCode = StatusCodes.Status404NotFound, Message = "User does not exist" } },
            { CustomCode.UserNotConfirmed, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "User account not confirmed" } },
            { CustomCode.InvalidCredentials, new MessageDetail {
                HttpCode = StatusCodes.Status401Unauthorized, Message = "Invalid credentials" } },
            { CustomCode.UserAccountLocked, new MessageDetail {
                HttpCode = StatusCodes.Status423Locked, Message = "User account is locked" } },
            { CustomCode.ConfirmEmailTokenInvalidOrExpired, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Confirm email token is invalid or expired" } },
            { CustomCode.UserAlreadyConfirmed, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "User account already confirmed" } },

            #endregion
        };
    }
}
