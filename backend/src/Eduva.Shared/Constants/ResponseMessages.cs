using Eduva.Shared.Enums;
using Eduva.Shared.Models;
using Microsoft.AspNetCore.Http;

namespace Eduva.Shared.Constants
{
    public static class ResponseMessages
    {
        internal static readonly Dictionary<CustomCode, MessageDetail> _messages = new Dictionary<CustomCode, MessageDetail>
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
            { CustomCode.RequiresOtpVerification, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "OTP verification required." } },
            { CustomCode.OtpSentSuccessfully, new MessageDetail {
                HttpCode = StatusCodes.Status200OK, Message = "OTP sent successfully" } },

            #endregion


            #region Error Codes
            { CustomCode.ModelInvalid, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Invalid model state" } },
            { CustomCode.Unauthorized, new MessageDetail {
                HttpCode = StatusCodes.Status401Unauthorized, Message = "Unauthorized" } },
            { CustomCode.Forbidden, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "Access denied" } },
            { CustomCode.SystemError, new MessageDetail {
                HttpCode = StatusCodes.Status500InternalServerError, Message = "An unexpected error occurred in the system" } },
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
            { CustomCode.NewPasswordSameAsOld, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "New password cannot be the same as the old password" } },
            { CustomCode.OtpInvalidOrExpired, new MessageDetail {
                HttpCode = StatusCodes.Status401Unauthorized, Message = "OTP code is invalid or has expired." } },
            { CustomCode.TwoFactorIsAlreadyEnabled, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Two-factor authentication is already enabled." } },
            { CustomCode.TwoFactorIsAlreadyDisabled, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Two-factor authentication is already disabled." } },
            { CustomCode.UserIdNotFound, new MessageDetail {
                HttpCode = StatusCodes.Status401Unauthorized, Message = "User ID not found in the request. Please ensure you are authenticated." } },
            { CustomCode.AccessTokenInvalidOrExpired, new MessageDetail {
                HttpCode = StatusCodes.Status401Unauthorized, Message = "Access token is invalid or has expired." } },
            { CustomCode.UserAlreadyHasSchool, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "User already has a school associated with their account." } },
            { CustomCode.SchoolNotFound, new MessageDetail {
                HttpCode = StatusCodes.Status404NotFound, Message = "School not found" } },
            { CustomCode.PlanNotFound, new MessageDetail {
                HttpCode = StatusCodes.Status404NotFound, Message = "Subscription plan not found" } },
            { CustomCode.PlanNotActive, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Subscription plan is not active" } },
            {CustomCode.SchoolSubscriptionNotFound, new MessageDetail {
                HttpCode = StatusCodes.Status404NotFound, Message = "School subscription not found" } },
            { CustomCode.PaymentFailed, new MessageDetail {
                HttpCode = StatusCodes.Status402PaymentRequired, Message = "Payment failed. Please try again." } },
            { CustomCode.PaymentAlreadyConfirmed, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Payment has already been confirmed." } },
            { CustomCode.DowngradeNotAllowed, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "You cannot switch to a lower-tier plan or a shorter billing cycle." } },
            { CustomCode.SchoolSubscriptionAlreadyExists, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "You are already subscribed to this plan and billing cycle." } },
            { CustomCode.ClassNotFound, new MessageDetail {
                HttpCode = StatusCodes.Status404NotFound, Message = "Class not found" } },
            { CustomCode.ClassNameAlreadyExists, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Class name already exists in this school" } },
            { CustomCode.ClassCodeDuplicate, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Class code is duplicated, please try again" } },
            { CustomCode.ClassCreateFailed, new MessageDetail {
                HttpCode = StatusCodes.Status500InternalServerError, Message = "Failed to create class" } },
            { CustomCode.ClassUpdateFailed, new MessageDetail {
                HttpCode = StatusCodes.Status500InternalServerError, Message = "Failed to update class" } },
            { CustomCode.ClassArchiveFailed, new MessageDetail {
                HttpCode = StatusCodes.Status500InternalServerError, Message = "Failed to archive class" } },

            // File Storage Errors
            { CustomCode.InvalidBlobName, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Invalid blob name provided." } },
            { CustomCode.InvalidBlobUrl, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Invalid blob URL provided." } },
            { CustomCode.BlobNotFound, new MessageDetail {
                HttpCode = StatusCodes.Status404NotFound, Message = "The specified blob was not found. Ensure the blob name is correct and try again." } },
            #endregion
        };

        public static IReadOnlyDictionary<CustomCode, MessageDetail> Messages => _messages;
    }
}