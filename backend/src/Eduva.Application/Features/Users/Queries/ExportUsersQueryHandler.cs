using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Schools.Responses;
using Eduva.Application.Features.Users.Requests;
using Eduva.Application.Features.Users.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace Eduva.Application.Features.Users.Queries
{
    public class ExportUsersQueryHandler : IRequestHandler<ExportUsersQuery, byte[]>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserRepository _userRepository;

        public ExportUsersQueryHandler(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            IUserRepository userRepository)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _userRepository = userRepository;
        }

        public async Task<byte[]> Handle(ExportUsersQuery request, CancellationToken cancellationToken)
        {
            ExcelPackage.License.SetNonCommercialPersonal("EDUVA");

            var schoolAdmin = await _userRepository.GetByIdWithSchoolAsync(request.SchoolAdminId, cancellationToken);
            if (schoolAdmin?.SchoolId == null)
            {
                throw new AppException(CustomCode.UserNotPartOfSchool);
            }

            var users = await GetUsersForExport(request.Request, schoolAdmin.SchoolId.Value);

            // Create Excel file
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Người dùng");

            // Set up headers
            SetupHeaders(worksheet);

            // Populate data
            PopulateData(worksheet, users);

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            // Return file bytes
            using var stream = new MemoryStream();
            await package.SaveAsAsync(stream, cancellationToken);
            return stream.ToArray();
        }

        private async Task<List<UserResponse>> GetUsersForExport(ExportUsersRequest request, int schoolId)
        {
            var users = new List<UserResponse>();

            if (request.Role.HasValue)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(request.Role.Value.ToString());
                var filteredUsers = usersInRole.Where(u => u.SchoolId == schoolId).ToList();

                if (request.Status.HasValue)
                {
                    filteredUsers = filteredUsers.Where(u => u.Status == request.Status.Value).ToList();
                }
                else
                {
                    filteredUsers = filteredUsers.Where(u => u.Status != EntityStatus.Deleted).ToList();
                }

                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var searchTerm = request.SearchTerm.ToLower();
                    filteredUsers = filteredUsers.Where(u =>
                        (u.FullName ?? "").ToLower().Contains(searchTerm) ||
                        (u.Email ?? "").ToLower().Contains(searchTerm)).ToList();
                }

                filteredUsers = SortUsers(filteredUsers, request.SortBy, request.SortDirection);

                var school = await _unitOfWork.GetRepository<School, int>().GetByIdAsync(schoolId);

                foreach (var user in filteredUsers)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var mapped = MapToUserResponse(user, roles.ToList(), school);
                    users.Add(mapped);
                }
            }
            else
            {
                var allowedRoles = new[] { nameof(Role.Teacher), nameof(Role.ContentModerator), nameof(Role.Student) };
                var allUsers = new List<ApplicationUser>();

                foreach (var role in allowedRoles)
                {
                    var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                    var schoolUsers = usersInRole.Where(u => u.SchoolId == schoolId).ToList();
                    allUsers.AddRange(schoolUsers);
                }

                allUsers = allUsers.GroupBy(u => u.Id).Select(g => g.First()).ToList();

                if (request.Status.HasValue)
                {
                    allUsers = allUsers.Where(u => u.Status == request.Status.Value).ToList();
                }
                else
                {
                    allUsers = allUsers.Where(u => u.Status != EntityStatus.Deleted).ToList();
                }

                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var searchTerm = request.SearchTerm.ToLower();
                    allUsers = allUsers.Where(u =>
                        (u.FullName ?? "").ToLower().Contains(searchTerm) ||
                        (u.Email ?? "").ToLower().Contains(searchTerm)).ToList();
                }

                allUsers = SortUsers(allUsers, request.SortBy, request.SortDirection);

                var school = await _unitOfWork.GetRepository<School, int>().GetByIdAsync(schoolId);

                foreach (var user in allUsers)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var mapped = MapToUserResponse(user, roles.ToList(), school);
                    users.Add(mapped);
                }
            }

            return users;
        }

        private static List<ApplicationUser> SortUsers(List<ApplicationUser> users, string sortBy, string sortDirection)
        {
            var isDesc = sortDirection.ToLower() == "desc";

            return sortBy.ToLower() switch
            {
                "fullname" => isDesc
                    ? users.OrderByDescending(u => u.FullName).ToList()
                    : users.OrderBy(u => u.FullName).ToList(),
                "email" => isDesc
                    ? users.OrderByDescending(u => u.Email).ToList()
                    : users.OrderBy(u => u.Email).ToList(),
                "status" => isDesc
                    ? users.OrderByDescending(u => u.Status).ToList()
                    : users.OrderBy(u => u.Status).ToList(),
                "createdat" => isDesc
                    ? users.OrderByDescending(u => u.CreatedAt).ToList()
                    : users.OrderBy(u => u.CreatedAt).ToList(),
                _ => isDesc
                    ? users.OrderByDescending(u => u.FullName).ToList()
                    : users.OrderBy(u => u.FullName).ToList()
            };
        }

        private static UserResponse MapToUserResponse(ApplicationUser user, List<string> roles, School? school)
        {
            return new UserResponse
            {
                Id = user.Id,
                FullName = user.FullName ?? "",
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                AvatarUrl = user.AvatarUrl,
                Roles = roles,
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                LastModifiedAt = user.LastModifiedAt,
                LastLoginAt = user.LastLoginAt,
                CreditBalance = user.TotalCredits,
                School = school != null ? new SchoolResponse
                {
                    Id = school.Id,
                    Name = school.Name,
                    Address = school.Address ?? "",
                    ContactEmail = school.ContactEmail,
                    ContactPhone = school.ContactPhone,
                    WebsiteUrl = school.WebsiteUrl ?? ""
                } : null
            };
        }

        private static void SetupHeaders(ExcelWorksheet worksheet)
        {
            // Set header style
            var headerRange = worksheet.Cells[1, 1, 1, 10];
            var headerStyle = headerRange.Style;
            headerStyle.Fill.PatternType = ExcelFillStyle.Solid;
            headerStyle.Fill.BackgroundColor.SetColor(Color.FromArgb(68, 114, 196));
            headerStyle.Font.Color.SetColor(Color.White);
            headerStyle.Font.Bold = true;
            headerStyle.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Set headers
            worksheet.Cells[1, 1].Value = "STT";
            worksheet.Cells[1, 2].Value = "Họ và tên";
            worksheet.Cells[1, 3].Value = "Email";
            worksheet.Cells[1, 4].Value = "Số điện thoại";
            worksheet.Cells[1, 5].Value = "Vai trò";
            worksheet.Cells[1, 6].Value = "Trạng thái";
            worksheet.Cells[1, 7].Value = "Số dư tín dụng";
            worksheet.Cells[1, 8].Value = "Ngày tạo";
            worksheet.Cells[1, 9].Value = "Lần đăng nhập cuối";
            worksheet.Cells[1, 10].Value = "Trường học";

            // Set column widths
            worksheet.Column(1).Width = 5;
            worksheet.Column(2).Width = 25;
            worksheet.Column(3).Width = 30;
            worksheet.Column(4).Width = 15;
            worksheet.Column(5).Width = 20;
            worksheet.Column(6).Width = 15;
            worksheet.Column(7).Width = 15;
            worksheet.Column(8).Width = 20;
            worksheet.Column(9).Width = 20;
            worksheet.Column(10).Width = 25;
        }

        private static void PopulateData(ExcelWorksheet worksheet, List<UserResponse> users)
        {
            TimeZoneInfo vnTimeZone;
            try
            {
                vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // Windows
            }
            catch
            {
                vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); // Linux
            }

            for (int i = 0; i < users.Count; i++)
            {
                var user = users[i];
                var row = i + 2; // Start from row 2 (row 1 is header)

                worksheet.Cells[row, 1].Value = i + 1; // STT
                worksheet.Cells[row, 2].Value = user.FullName;
                worksheet.Cells[row, 3].Value = user.Email;
                worksheet.Cells[row, 4].Value = user.PhoneNumber ?? "Chưa cập nhật";
                worksheet.Cells[row, 5].Value = GetRoleDisplayName(user.Roles);
                worksheet.Cells[row, 6].Value = GetStatusDisplayName(user.Status);
                worksheet.Cells[row, 7].Value = user.CreditBalance;
                DateTimeOffset createdAt = user.CreatedAt;
                DateTimeOffset createdAtVn = TimeZoneInfo.ConvertTime(createdAt, vnTimeZone);
                worksheet.Cells[row, 8].Value = createdAtVn.ToString("HH:mm:ss dd/MM/yyyy");
                if (user.LastLoginAt.HasValue)
                {
                    DateTimeOffset lastLogin = user.LastLoginAt.Value;
                    DateTimeOffset lastLoginVn = TimeZoneInfo.ConvertTime(lastLogin, vnTimeZone);
                    worksheet.Cells[row, 9].Value = lastLoginVn.ToString("HH:mm:ss dd/MM/yyyy");
                }
                else
                {
                    worksheet.Cells[row, 9].Value = "Chưa đăng nhập";
                }
                worksheet.Cells[row, 10].Value = user.School?.Name ?? "";

                // Set status color
                var statusCell = worksheet.Cells[row, 6];
                if (user.Status == EntityStatus.Active)
                {
                    statusCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    statusCell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(198, 239, 206));
                }
                else if (user.Status == EntityStatus.Inactive)
                {
                    statusCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    statusCell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 199, 206));
                }
            }
        }

        private static string GetRoleDisplayName(List<string> roles)
        {
            var roleDisplayNames = new Dictionary<string, string>
            {
                { "SchoolAdmin", "Quản trị viên trường học" },
                { "Teacher", "Giáo viên" },
                { "ContentModerator", "Kiểm duyệt viên" },
                { "Student", "Học sinh" }
            };

            var displayNames = roles
                .Where(role => roleDisplayNames.ContainsKey(role))
                .Select(role => roleDisplayNames[role]);

            return string.Join(", ", displayNames);
        }

        private static string GetStatusDisplayName(EntityStatus status)
        {
            return status switch
            {
                EntityStatus.Active => "Đang hoạt động",
                EntityStatus.Inactive => "Vô hiệu hóa",
                EntityStatus.Deleted => "Đã xóa",
                _ => "Không xác định"
            };
        }
    }
}