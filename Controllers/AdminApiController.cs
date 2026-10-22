using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using nhdphuong_1787_api.Models;

namespace nhdphuong_1787_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AdminApiController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AdminApiController(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        private bool IsAdmin() =>
            User.IsInRole("Admin") ||
            User.HasClaim(ClaimTypes.Role, "Admin") ||
            User.HasClaim("role", "Admin") ||
            User.HasClaim("Role", "Admin") ||
            User.Identity?.Name?.ToLower() == "admin1787";

        // GET: api/AdminApi/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetSystemStats()
        {
            if (!IsAdmin()) return Forbid();

            var totalUsers = await _userManager.Users.CountAsync();
            var totalChildren = await _context.ChildProfiles.CountAsync();
            var onlineChildren = await _context.ChildProfiles.CountAsync(c => c.IsOnline);
            var lockedChildren = await _context.ChildProfiles.CountAsync(c => c.IsLocked);
            var totalRules = await _context.AppRules.CountAsync();
            var totalSchedules = await _context.ScreenTimeSchedules.CountAsync();
            var totalAlerts = await _context.AlertLogs.CountAsync();

            return Ok(new
            {
                totalUsers,
                totalChildren,
                onlineChildren,
                lockedChildren,
                totalRules,
                totalSchedules,
                totalAlerts
            });
        }

        // GET: api/AdminApi/users
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            if (!IsAdmin()) return Forbid();

            var users = await _userManager.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
            var result = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var children = await _context.ChildProfiles
                    .Where(c => c.ParentId == user.Id)
                    .Select(c => new
                    {
                        c.Id,
                        c.FullName,
                        c.DeviceName,
                        c.IsOnline,
                        c.BatteryLevel,
                        c.CurrentNoiseDb,
                        c.CurrentEyeDistanceCm
                    })
                    .ToListAsync();

                result.Add(new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    user.FullName,
                    user.PhoneNumber,
                    user.CreatedAt,
                    Role = roles.FirstOrDefault() ?? "Customer",
                    ChildrenCount = children.Count,
                    Children = children
                });
            }

            return Ok(result);
        }

        // POST: api/AdminApi/users
        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateAdminUserModel model)
        {
            if (!IsAdmin()) return Forbid();

            if (string.IsNullOrWhiteSpace(model.UserName))
                return BadRequest(new { message = "Tên đăng nhập không được để trống!" });

            var existing = await _userManager.FindByNameAsync(model.UserName);
            if (existing != null)
                return BadRequest(new { message = "Tên đăng nhập đã tồn tại trong hệ thống!" });

            var user = new User
            {
                UserName = model.UserName,
                Email = string.IsNullOrWhiteSpace(model.Email) ? $"{model.UserName}@childguard.local" : model.Email,
                FullName = string.IsNullOrWhiteSpace(model.FullName) ? model.UserName : model.FullName,
                PhoneNumber = model.PhoneNumber,
                CreatedAt = DateTime.UtcNow
            };

            var createRes = await _userManager.CreateAsync(user, string.IsNullOrWhiteSpace(model.Password) ? "Password123@" : model.Password);
            if (!createRes.Succeeded)
            {
                var errors = string.Join(", ", createRes.Errors.Select(e => e.Description));
                return BadRequest(new { message = errors });
            }

            var role = string.IsNullOrWhiteSpace(model.Role) ? "Customer" : model.Role;
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));

            await _userManager.AddToRoleAsync(user, role);

            return Ok(new { message = "Tạo tài khoản người dùng thành công!", userId = user.Id });
        }

        // DELETE: api/AdminApi/users/{id}
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (!IsAdmin()) return Forbid();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng!" });

            if (user.UserName?.ToLower() == "admin1787")
                return BadRequest(new { message = "Không thể xóa tài khoản Quản trị viên tối cao (admin1787)!" });

            // Xóa cascade toàn bộ hồ sơ thiết bị của người dùng này
            var children = await _context.ChildProfiles.Where(c => c.ParentId == id).ToListAsync();
            _context.ChildProfiles.RemoveRange(children);
            await _context.SaveChangesAsync();

            await _userManager.DeleteAsync(user);
            return Ok(new { message = $"Đã xóa người dùng {user.UserName} và toàn bộ thiết bị liên quan!" });
        }

        // GET: api/AdminApi/children (Toàn bộ thiết bị kèm thông tin phụ huynh)
        [HttpGet("children")]
        public async Task<IActionResult> GetAllChildrenWithParents()
        {
            if (!IsAdmin()) return Forbid();

            var children = await _context.ChildProfiles
                .Include(c => c.Parent)
                .Include(c => c.AppRules)
                .Include(c => c.Schedules)
                .Include(c => c.AlertLogs)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();

            return Ok(children);
        }
    }

    public class CreateAdminUserModel
    {
        public string UserName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Password { get; set; } = "Password123@";
        public string? Role { get; set; } = "Customer"; // Admin, Staff, Customer
    }
}
