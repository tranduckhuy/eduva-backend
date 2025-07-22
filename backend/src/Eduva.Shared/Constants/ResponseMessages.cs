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
                HttpCode = StatusCodes.Status200OK, Message = "OTP verification required." } },
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
            { CustomCode.SchoolAlreadyArchived, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "School is already archived" } },
            { CustomCode.SubscriptionPlanMustBeArchived, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Subscription plan must be archived to perform this action" } },
            { CustomCode.PlanInUse, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "This plan is currently in use and cannot be modified or deleted." } },
            { CustomCode.SchoolAlreadyActive, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "School is already active" } },
            { CustomCode.PlanAlreadyActive, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Plan is already active" } },
            { CustomCode.PlanAlreadyArchived, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Plan is already archived" } },
            { CustomCode.InvalidRestrictedRole, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Invalid restricted role provided. Please use a valid role." } },
            { CustomCode.UserNotPartOfSchool, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "User is not part of the school. Please ensure the user is registered in the school." } },
            { CustomCode.FileIsRequired, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "File is required for this operation." } },
            { CustomCode.InvalidFileType, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Invalid file type. Please upload a valid file." } },
            { CustomCode.UserAlreadyLocked, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "User account is already locked." } },
            { CustomCode.CannotLockSelf, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "You cannot lock your own account." } },
            { CustomCode.CannotUnlockSelf, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "You cannot unlock your own account." } },
            { CustomCode.UserNotLocked, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "User account is not locked." } },
            { CustomCode.InvalidTemplateType, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Invalid template type provided. Please use a valid template type." } },
            { CustomCode.FileDownloadFailed, new MessageDetail {
                HttpCode = StatusCodes.Status500InternalServerError, Message = "Failed to download the file. Please try again later." } },
            { CustomCode.IncorrectCurrentPassword, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "The current password provided is incorrect. Please try again." } },
            { CustomCode.OtpResendTooSoon, new MessageDetail {
                HttpCode = StatusCodes.Status429TooManyRequests, Message = "You have requested an OTP too soon. Please wait 120s before requesting a new OTP." } },
            { CustomCode.UserNotFound, new MessageDetail {
                HttpCode = StatusCodes.Status404NotFound, Message = "User not found. Please check the user ID and try again." } },
            { CustomCode.UserAlreadyDeleted, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "User account has already been deleted." } },
            { CustomCode.UserMustBeLockedBeforeDelete, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "User account must be locked before it can be deleted." } },
            { CustomCode.CannotDeleteYourOwnAccount, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "You cannot delete your own account. Please contact support for assistance." } },
            { CustomCode.CannotUnlockDeletedUser, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Cannot unlock a deleted user account. Please check the user status." } },
            { CustomCode.CannotViewUserFromDifferentSchool, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "You cannot view users from a different school. Please ensure you are viewing users within your own school." } },
            { CustomCode.CannotViewRestrictedUserRoles, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "You do not have permission to view restricted user roles." } },
            { CustomCode.CannotModifyRestrictedUserRoles, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "You do not have permission to modify restricted user roles." } },
            { CustomCode.CannotModifyOwnRoles, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "You cannot modify your own roles. Please contact support for assistance." } },
            { CustomCode.CannotManageUserFromDifferentSchool, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "You cannot manage users from a different school. Please ensure you are managing users within your own school." } },
            { CustomCode.InvalidRoleCombination, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Invalid role combination. Only ContentModerator and Teacher roles can be combined within a school." } },
            { CustomCode.InsufficientPermissionToManageRoles, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "You do not have sufficient permissions to manage user roles." } },
            { CustomCode.RoleRemovalFailed, new MessageDetail {
                HttpCode = StatusCodes.Status500InternalServerError, Message = "Failed to remove user role. Please try again later." } },
            { CustomCode.RoleAssignmentFailed, new MessageDetail {
                HttpCode = StatusCodes.Status500InternalServerError, Message = "Failed to assign user role. Please try again later." } },
             { CustomCode.RoleListEmpty, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "At least one role must be specified for assignment." } },
            { CustomCode.RestrictedRoleNotAllowed, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "SystemAdmin and SchoolAdmin roles cannot be assigned by School Admin." } },
            { CustomCode.InvalidSingleRole, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Invalid role specified. Only Student, Teacher, or ContentModerator roles are allowed." } },
            { CustomCode.InvalidMultipleRoleCombination, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Invalid role combination. Only ContentModerator and Teacher roles can be combined." } },
            { CustomCode.TooManyRolesAssigned, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Too many roles specified. Maximum 2 roles allowed with specific combinations only." } },
            { CustomCode.StudentRoleNotAssignable, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Student role cannot be assigned through role management. Students are created through registration or import process." } },

            // File Storage Errors
            { CustomCode.InvalidBlobName, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Invalid blob name provided." } },
            { CustomCode.InvalidBlobUrl, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Invalid blob URL provided." } },
            { CustomCode.BlobNotFound, new MessageDetail {
                HttpCode = StatusCodes.Status404NotFound, Message = "The specified blob was not found. Ensure the blob name is correct and try again." } },
                
            // Student Class Errors
            { CustomCode.UserNotStudent, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "User is not a student. Only students can enroll in classes." } },
            { CustomCode.ClassNotActive, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Class is not active. You can only enroll in active classes." } },
            { CustomCode.StudentAlreadyEnrolled, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "You are already enrolled in this class." } },
            { CustomCode.StudentCannotEnrollDifferentSchool, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "You cannot enroll in a class from a different school." } },
            { CustomCode.EnrollmentFailed, new MessageDetail {
                HttpCode = StatusCodes.Status500InternalServerError, Message = "Failed to enroll in the class. Please try again later." } },
            { CustomCode.DuplicateClassCodeSameSchool, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "The class code already exists in this school. Please use a different class code." } },

            // Payment Transaction Errors
            { CustomCode.PaymentTransactionNotFound, new MessageDetail {
                HttpCode = StatusCodes.Status404NotFound, Message = "Payment transaction not found" } },
            { CustomCode.AICreditPackNotFound, new MessageDetail {
                HttpCode = StatusCodes.Status404NotFound, Message = "AI credit pack not found" } },
            { CustomCode.AICreditPackAlreadyArchived, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "AI credit pack is already archived" } },
            { CustomCode.AICreditPackAlreadyActive, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "AI credit pack is already active" } },
            { CustomCode.AICreditPackMustBeArchived, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "AI credit pack must be archived to perform this action" } },
            { CustomCode.AICreditPackNotActive, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "AI credit pack is not active" } },
            { CustomCode.InvalidPaymentPurpose, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Invalid payment purpose. Please check the payment details and try again." } },
            { CustomCode.CreditTransactionNotFound, new MessageDetail {
                HttpCode = StatusCodes.Status404NotFound, Message = "Credit transaction not found" } },

            // Subscription Errors
            { CustomCode.SchoolAndSubscriptionRequired, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "Forbidden. You must complete school and subscription information to access this resource." } },
            { CustomCode.SubscriptionExpiredWithDataLossRisk, new MessageDetail {
                HttpCode = StatusCodes.Status402PaymentRequired, Message = "Your subscription has expired and you are at risk of data loss. Please renew your subscription to continue using the service." } },

            // Forbidden
            { CustomCode.SchoolInactive, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "School is inactive. Please contact support for assistance." } },
            { CustomCode.SubscriptionInvalid, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "Subscription is invalid. Please check your subscription status." } },
            { CustomCode.ExceedUserLimit, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "You have exceeded the maximum number of users allowed by your subscription plan." } },

            // Class Errors
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
            { CustomCode.ClassCodeResetFailed, new MessageDetail {
                HttpCode = StatusCodes.Status500InternalServerError, Message = "Failed to reset class code" } },
            { CustomCode.ClassAlreadyArchived, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Class is already archived" } },
            { CustomCode.ClassNotArchived, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Class is not archived and cannot be restored" } },
            { CustomCode.ClassMustBeArchivedBeforeDelete, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Class must be archived before it can be permanently deleted" } },
            { CustomCode.CannotCreateClassForInactiveSchool, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Cannot create classes for inactive schools" } },
            { CustomCode.ClassRestoreFailed, new MessageDetail {
                HttpCode = StatusCodes.Status500InternalServerError, Message = "Failed to restore class" } },
            { CustomCode.ClassDeleteFailed, new MessageDetail {
                HttpCode = StatusCodes.Status500InternalServerError, Message = "Failed to delete class" } },
            { CustomCode.ClassNameAlreadyExistsForTeacher, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "You already have a class with this name" } },
            { CustomCode.NotTeacherOfClass, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "You do not have permission to modify this class as you are not the teacher of this class" } },
            { CustomCode.NotAdminForClassList, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "Only system administrators and school administrators can view all classes" } },
            { CustomCode.StudentNotEnrolled, new MessageDetail {
                HttpCode = StatusCodes.Status404NotFound, Message = "Student is not enrolled in this class" } },
            { CustomCode.StudentRemovalFailed, new MessageDetail {
                HttpCode = StatusCodes.Status500InternalServerError, Message = "Failed to remove student from class" } },
            { CustomCode.StudentNotFoundInClass, new MessageDetail {
                HttpCode = StatusCodes.Status404NotFound, Message = "Student not found in this class" } },

            // Folder Errors
            { CustomCode.FolderNotFound, new MessageDetail {
                HttpCode = StatusCodes.Status404NotFound, Message = "Folder not found" } },
            { CustomCode.FolderNameAlreadyExists, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Folder name already exists" } },
            { CustomCode.FolderCreateFailed, new MessageDetail {
                HttpCode = StatusCodes.Status500InternalServerError, Message = "Failed to create folder" } },
            { CustomCode.FolderUpdateFailed, new MessageDetail {
                HttpCode = StatusCodes.Status500InternalServerError, Message = "Failed to update folder" } },
            { CustomCode.FolderDeleteFailed, new MessageDetail {
                HttpCode = StatusCodes.Status500InternalServerError, Message = "Failed to delete folder" } },
            { CustomCode.FolderArchiveFailed, new MessageDetail {
                HttpCode = StatusCodes.Status500InternalServerError, Message = "Failed to archive folder" } },
            { CustomCode.FolderRestoreFailed, new MessageDetail {
                HttpCode = StatusCodes.Status500InternalServerError, Message = "Failed to restore folder" } },
            { CustomCode.FolderAlreadyArchived, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Folder is already archived" } },
            { CustomCode.FolderAlreadyActive, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Folder is already active" } },
            { CustomCode.FolderShouldBeArchivedBeforeDelete, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Folder must be archived before it can be permanently deleted" } },
            {CustomCode.LessonMaterialNotFoundInFolder, new MessageDetail{
                HttpCode = StatusCodes.Status400BadRequest, Message = "Lesson Material not found in this folder"} },
            { CustomCode.FolderMustBePersonal, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Folder must be personal" }},
            { CustomCode.FolderShouldBeArchivedBeforeRestore, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Folder must be archived before it can be restored." }},

              // Question Error
            { CustomCode.InsufficientPermissionToCreateQuestion, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "You do not have sufficient permissions to create a question for this lesson material." } },
            { CustomCode.LessonMaterialNotFound, new MessageDetail {
                HttpCode = StatusCodes.Status404NotFound, Message = "Lesson material not found. Please check the lesson material ID and try again." } },
            { CustomCode.LessonMaterialNotActive, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Lesson material is not active. You can only create questions for active lesson materials." } },
            { CustomCode.InsufficientPermission, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "You do not have sufficient permissions to perform this action." } },
            { CustomCode.CannotCreateQuestionForLessonNotInYourSchool, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "You cannot create a question for a lesson material that is not in your school." } },
            { CustomCode.CannotCreateQuestionForPendingLesson, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "You cannot create a question for a lesson material that is pending approval." } },
            { CustomCode.StudentNotEnrolledInAnyClass, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "You must be enrolled in a class to create questions for lesson materials." } },
            { CustomCode.CannotCreateQuestionForLessonNotAccessible, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "You cannot create a question for a lesson material that is not accessible to you." } },
            { CustomCode.QuestionNotFound, new MessageDetail {
                HttpCode = StatusCodes.Status404NotFound, Message = "Question not found. Please check the question ID and try again." } },
            { CustomCode.QuestionNotActive, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Question is not active. You can only update active questions." } },
            { CustomCode.InsufficientPermissionToUpdateQuestion, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "You do not have sufficient permissions to update this question." } },
            { CustomCode.InsufficientPermissionToDeleteQuestion, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "You do not have sufficient permissions to delete this question." } },
            { CustomCode.CannotDeleteQuestionWithComments, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Cannot delete question that has comments. Please remove all comments first." } },
            { CustomCode.TeacherHasNoActiveClasses, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "Teacher has no active classes to manage questions." } },
            { CustomCode.StudentNotInTeacherClasses, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "Student is not enrolled in any of your classes." } },
            { CustomCode.StudentNotEnrolledInAnyClassForQuestions, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "You must be enrolled in at least one class to view questions." } },
            { CustomCode.StudentNotInSchoolClass, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "You are not enrolled in any class within your school." } },
            { CustomCode.MaterialNotAccessibleToStudent, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "You do not have access to this lesson material. Please check your class enrollment." } },
            { CustomCode.StudentNotEnrolledInClassWithMaterial, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "You are not enrolled in any class that contains this lesson material." } },
            { CustomCode.TeacherMustHaveActiveClass, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "Teachers must have at least one active class to view questions." } },
            { CustomCode.TeacherNotHaveAccessToMaterial, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "You do not have access to this lesson material through your classes." } },
            { CustomCode.TeacherClassNotInOwnSchool, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "Teacher classes must belong to your own school." } },

             // Comment Errors
            { CustomCode.CommentNotFound, new MessageDetail {
                HttpCode = StatusCodes.Status404NotFound, Message = "Comment not found" } },
            { CustomCode.CommentNotActive, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Comment is not active" } },
            { CustomCode.InsufficientPermissionToCreateComment, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "You don't have permission to create comments on this question" } },
            { CustomCode.InsufficientPermissionToUpdateComment, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "You don't have permission to update this comment" } },
            { CustomCode.InsufficientPermissionToDeleteComment, new MessageDetail {
                HttpCode = StatusCodes.Status403Forbidden, Message = "You don't have permission to delete this comment" } },
            { CustomCode.CannotDeleteCommentWithReplies, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Cannot delete comment that has replies" } },
            { CustomCode.ParentCommentNotFound, new MessageDetail {
                HttpCode = StatusCodes.Status404NotFound, Message = "Parent comment not found or is not a top-level comment" } },
            { CustomCode.CommentContentRequired, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Comment content is required" } },

            //Lesson Material
            { CustomCode.LessonMaterialAlreadyApproved, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "This lesson material has already been approved."} },
            { CustomCode.LessonMaterialAlreadyRejected, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "This lesson material has already been rejected."} },
            { CustomCode.LessonMaterialNotApproved, new MessageDetail
            {   HttpCode = StatusCodes.Status400BadRequest, Message = "This lesson material has not been approved yet."} },
            { CustomCode.ReasonIsRequiredWhenRejectingLessonMaterial, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "You must enter the reason when refusing to approve the lesson!"} },
            { CustomCode.LessonMaterialRestoreFailed, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Failed to restore the lesson material!" }},
            { CustomCode.InvalidPersonalFolder, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "The folder must be a personal folder and in Active status." }},
            { CustomCode.LessonMaterialAlreadyExistsInClassFolder, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "The lesson material already exists in another folder of this class." }},

            // Storage Quota Errors
            { CustomCode.StorageQuotaExceeded, new MessageDetail {
                HttpCode = StatusCodes.Status413PayloadTooLarge, Message = "Storage quota exceeded. Please upgrade your subscription or delete some files" } },


            // Job Errors
            { CustomCode.JobNotFound, new MessageDetail {
                HttpCode = StatusCodes.Status404NotFound, Message = "Job not found" } },
            { CustomCode.JobContentNotGenerated, new MessageDetail {
                HttpCode = StatusCodes.Status400BadRequest, Message = "Job content must be generated before confirmation." } },

            // User credit
            { CustomCode.InsufficientUserCredit, new MessageDetail {
                HttpCode = StatusCodes.Status402PaymentRequired, Message = "User credit is insufficient to perform this operation."} },
            // Notification Errors
            { CustomCode.NotificationNotFound, new MessageDetail {
                HttpCode = StatusCodes.Status404NotFound, Message = "Notification not found" } },
           
            #endregion
        };

        public static IReadOnlyDictionary<CustomCode, MessageDetail> Messages => _messages;
    }
}