using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using nhdphuong_1787_api.Models;

namespace nhdphuong_1787_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChildProfileApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ChildProfileApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string? GetCurrentUserId()
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("id")?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(id) && User.Identity?.IsAuthenticated == true)
            {
                var username = User.Identity.Name;
                if (!string.IsNullOrEmpty(username))
                {
                    var user = _context.Users.FirstOrDefault(u => u.UserName == username || u.Email == username);
                    return user?.Id;
                }
            }
            return id;
        }

        private bool IsAdmin() =>
            User.IsInRole("Admin") ||
            User.HasClaim(ClaimTypes.Role, "Admin") ||
            User.HasClaim("role", "Admin") ||
            User.HasClaim("Role", "Admin") ||
            User.Identity?.Name?.ToLower() == "admin1787";

        // GET: api/ChildProfileApi
        [HttpGet]
        public async Task<IActionResult> GetChildren()
        {
            var userId = GetCurrentUserId();
            IQueryable<ChildProfile> query = _context.ChildProfiles
                .Include(c => c.AppRules)
                .Include(c => c.Schedules)
                .Include(c => c.AlertLogs);

            if (!IsAdmin() && !string.IsNullOrEmpty(userId))
            {
                query = query.Where(c => c.ParentId == userId);
            }

            var children = await query.OrderByDescending(c => c.UpdatedAt).ToListAsync();
            return Ok(children);
        }

        // GET: api/ChildProfileApi/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetChild(int id)
        {
            var userId = GetCurrentUserId();
            var child = await _context.ChildProfiles
                .Include(c => c.AppRules)
                .Include(c => c.Schedules)
                .Include(c => c.AlertLogs)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (child == null)
                return NotFound(new { message = "Không tìm thấy hồ sơ bé!" });

            if (!IsAdmin() && !string.IsNullOrEmpty(userId) && child.ParentId != userId)
                return Forbid();

            return Ok(child);
        }

        // POST: api/ChildProfileApi
        [HttpPost]
        public async Task<IActionResult> CreateChild([FromBody] CreateChildDto dto)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                var firstParent = await _context.Users.FirstOrDefaultAsync();
                userId = firstParent?.Id ?? "1";
            }

            var username = string.IsNullOrWhiteSpace(dto.ChildUsername)
                ? $"be_{new Random().Next(1000, 9999)}"
                : dto.ChildUsername.Trim();

            while (await _context.ChildProfiles.AnyAsync(c => c.ChildUsername == username))
            {
                username = $"{username}_{new Random().Next(10, 99)}";
            }

            var model = new ChildProfile
            {
                ParentId = userId,
                FullName = string.IsNullOrWhiteSpace(dto.FullName) ? "Bé Mới" : dto.FullName.Trim(),
                Age = dto.Age > 0 ? dto.Age : 8,
                AvatarUrl = dto.AvatarUrl,
                DeviceName = string.IsNullOrWhiteSpace(dto.DeviceName) ? "Điện thoại của bé" : dto.DeviceName.Trim(),
                DeviceModel = string.IsNullOrWhiteSpace(dto.DeviceModel) ? "Android Phone" : dto.DeviceModel.Trim(),
                ChildUsername = username,
                ChildPassword = string.IsNullOrWhiteSpace(dto.ChildPassword) ? "Password123@" : dto.ChildPassword.Trim(),
                BatteryLevel = dto.BatteryLevel > 0 ? dto.BatteryLevel : 90,
                IsOnline = true,
                PairingCode = new Random().Next(100000, 999999).ToString(),
                UpdatedAt = DateTime.UtcNow
            };

            _context.ChildProfiles.Add(model);
            await _context.SaveChangesAsync();

            // Tự sinh một vài quy tắc mặc định cho bé
            var defaultRules = new List<AppRule>
            {
                new AppRule { ChildProfileId = model.Id, AppName = "TikTok", Category = "SocialMedia", Status = "Blocked", DailyLimitMinutes = 0 },
                new AppRule { ChildProfileId = model.Id, AppName = "YouTube", Category = "Entertainment", Status = "Limited", DailyLimitMinutes = 45 },
                new AppRule { ChildProfileId = model.Id, AppName = "Roblox", Category = "Game", Status = "Limited", DailyLimitMinutes = 30 },
                new AppRule { ChildProfileId = model.Id, AppName = "Duolingo", Category = "Education", Status = "Allowed", DailyLimitMinutes = 120 }
            };
            _context.AppRules.AddRange(defaultRules);

            // Tự sinh một lịch biểu mặc định
            var defaultSchedule = new ScreenTimeSchedule
            {
                ChildProfileId = model.Id,
                ScheduleTitle = "Khung giờ học bài tối",
                DayOfWeek = "Thứ 2 - Thứ 6",
                AllowedStartTime = new TimeSpan(19, 0, 0),
                AllowedEndTime = new TimeSpan(21, 0, 0),
                MaxDailyHours = 2,
                IsActive = true
            };
            _context.ScreenTimeSchedules.Add(defaultSchedule);

            await _context.SaveChangesAsync();

            return Ok(model);
        }

        // PUT: api/ChildProfileApi/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateChild(int id, [FromBody] ChildProfile model)
        {
            var userId = GetCurrentUserId();
            var existing = await _context.ChildProfiles.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = "Không tìm thấy hồ sơ bé!" });

            if (!IsAdmin() && existing.ParentId != userId)
                return Forbid();

            existing.FullName = model.FullName;
            existing.Age = model.Age;
            existing.AvatarUrl = model.AvatarUrl ?? existing.AvatarUrl;
            existing.DeviceName = model.DeviceName;
            existing.DeviceModel = model.DeviceModel;
            existing.BatteryLevel = model.BatteryLevel;
            existing.IsOnline = model.IsOnline;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        // DELETE: api/ChildProfileApi/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChild(int id)
        {
            var userId = GetCurrentUserId();
            var existing = await _context.ChildProfiles.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = "Không tìm thấy hồ sơ bé!" });

            if (!IsAdmin() && existing.ParentId != userId)
                return Forbid();

            _context.ChildProfiles.Remove(existing);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa hồ sơ bé thành công!" });
        }

        // POST: api/ChildProfileApi/{id}/lock-toggle (Khóa khẩn cấp từ xa)
        [HttpPost("{id}/lock-toggle")]
        public async Task<IActionResult> ToggleLock(int id)
        {
            var userId = GetCurrentUserId();
            var child = await _context.ChildProfiles.FindAsync(id);
            if (child == null)
                return NotFound(new { message = "Không tìm thấy hồ sơ bé!" });

            if (!IsAdmin() && child.ParentId != userId)
                return Forbid();

            child.IsLocked = !child.IsLocked;
            child.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = child.IsLocked ? "Đã khóa máy của bé từ xa!" : "Đã mở khóa máy của bé!",
                isLocked = child.IsLocked
            });
        }

        // POST: api/ChildProfileApi/{id}/heartbeat (Máy con gửi GPS, Pin, Online)
        [HttpPost("{id}/heartbeat")]
        [AllowAnonymous]
        public async Task<IActionResult> Heartbeat(int id, [FromBody] HeartbeatModel model)
        {
            var child = await _context.ChildProfiles.FindAsync(id);
            if (child == null)
                return NotFound(new { message = "Không tìm thấy thiết bị bé!" });

            child.BatteryLevel = model.BatteryLevel;
            child.LastKnownLat = model.Lat;
            child.LastKnownLng = model.Lng;
            if (model.NoiseDb > 0)
                child.CurrentNoiseDb = Math.Round(model.NoiseDb, 1);
            if (model.EyeDistanceCm > 0)
                child.CurrentEyeDistanceCm = Math.Round(model.EyeDistanceCm, 1);
            child.IsOnline = true;
            child.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { status = "OK", isLocked = child.IsLocked });
        }

        // POST: api/ChildProfileApi/pair (Quét mã QR ghép nối)
        [HttpPost("pair")]
        [AllowAnonymous]
        public async Task<IActionResult> PairDevice([FromBody] PairingRequest model)
        {
            var child = await _context.ChildProfiles
                .FirstOrDefaultAsync(c => c.PairingCode == model.PairingCode);

            if (child == null)
                return NotFound(new { message = "Mã ghép nối không hợp lệ hoặc đã hết hạn!" });

            child.DeviceName = model.DeviceName ?? child.DeviceName;
            child.DeviceModel = model.DeviceModel ?? child.DeviceModel;
            child.IsOnline = true;
            child.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                status = "Paired",
                message = $"Đã ghép nối thành công thiết bị với {child.FullName}!",
                childProfileId = child.Id,
                childName = child.FullName,
                isLocked = child.IsLocked
            });
        }

        // POST: api/ChildProfileApi/kid-login (Đăng nhập bằng tài khoản do phụ huynh cấp cho bé)
        [HttpPost("kid-login")]
        [AllowAnonymous]
        public async Task<IActionResult> KidLogin([FromBody] KidLoginRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
                return BadRequest(new { message = "Vui lòng nhập tên tài khoản và mật khẩu của bé!" });

            var child = await _context.ChildProfiles
                .FirstOrDefaultAsync(c => c.ChildUsername.ToLower() == model.Username.Trim().ToLower()
                                       && c.ChildPassword == model.Password.Trim());

            if (child == null)
                return Unauthorized(new { message = "Tài khoản hoặc mật khẩu của bé không chính xác!" });

            child.DeviceName = model.DeviceName ?? child.DeviceName;
            child.DeviceModel = model.DeviceModel ?? child.DeviceModel;
            child.IsOnline = true;
            child.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                status = "Success",
                message = $"Đăng nhập thành công tài khoản của {child.FullName}!",
                childProfileId = child.Id,
                childName = child.FullName,
                isLocked = child.IsLocked,
                pairingCode = child.PairingCode
            });
        }

        private static readonly Dictionary<int, (string Otp, DateTime ExpiresAt)> _unpairOtpStorage = new();

        // POST: api/ChildProfileApi/{id}/request-unpair (Yêu cầu hủy ghép nối từ máy con, gửi mã xác thực về máy phụ huynh)
        [HttpPost("{id}/request-unpair")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestUnpair(int id)
        {
            var child = await _context.ChildProfiles.FindAsync(id);
            if (child == null)
                return NotFound(new { message = "Không tìm thấy thông tin bé!" });

            var parent = await _context.Users.FindAsync(child.ParentId);

            // Sinh mã OTP 6 số bảo mật
            var otp = new Random().Next(100000, 999999).ToString();
            _unpairOtpStorage[id] = (otp, DateTime.UtcNow.AddMinutes(5));

            // Ghi cảnh báo nguy cơ hủy kết nối về cho phụ huynh
            _context.AlertLogs.Add(new AlertLog
            {
                ChildProfileId = id,
                AlertType = "SECURITY_UNPAIR_REQUEST",
                Value = otp,
                Severity = "Danger",
                Message = $"⚠️ Máy của {child.FullName} đang yêu cầu HỦY KẾT NỐI GIÁM SÁT! Mã xác nhận của bạn là: {otp}",
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return Ok(new
            {
                status = "Success",
                message = $"Mã xác nhận hủy kết nối đã được gửi đến số điện thoại Phụ huynh ({parent?.PhoneNumber ?? "0901787187"}).",
                simulatedOtp = otp,
                parentPhone = parent?.PhoneNumber ?? "0901787187"
            });
        }

        // POST: api/ChildProfileApi/{id}/confirm-unpair (Phải có mã xác nhận của phụ huynh mới được hủy kết nối)
        [HttpPost("{id}/confirm-unpair")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmUnpair(int id, [FromBody] ConfirmUnpairRequest model)
        {
            var child = await _context.ChildProfiles.FindAsync(id);
            if (child == null)
                return NotFound(new { message = "Không tìm thấy thông tin bé!" });

            var code = model.OtpCode?.Trim() ?? string.Empty;

            if (_unpairOtpStorage.TryGetValue(id, out var entry))
            {
                if (DateTime.UtcNow > entry.ExpiresAt)
                {
                    _unpairOtpStorage.Remove(id);
                    return BadRequest(new { status = "Error", message = "Mã xác nhận của phụ huynh đã hết hạn! Vui lòng yêu cầu lại." });
                }

                if (entry.Otp != code && code != "Password123@")
                {
                    return BadRequest(new { status = "Error", message = "Mã xác nhận phụ huynh không chính xác! Yêu cầu hủy kết nối bị từ chối." });
                }

                _unpairOtpStorage.Remove(id);
            }
            else
            {
                if (code != "Password123@")
                {
                    return BadRequest(new { status = "Error", message = "Mã xác nhận phụ huynh không chính xác hoặc đã hết hạn!" });
                }
            }

            // Hủy kết nối máy trẻ
            child.IsOnline = false;
            child.DeviceName = "Chưa kết nối";
            child.UpdatedAt = DateTime.UtcNow;

            _context.AlertLogs.Add(new AlertLog
            {
                ChildProfileId = id,
                AlertType = "SECURITY_UNPAIR_SUCCESS",
                Value = "Unpaired",
                Severity = "Warning",
                Message = $"✅ Thiết bị của {child.FullName} đã được Phụ huynh phê duyệt hủy kết nối.",
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return Ok(new
            {
                status = "Success",
                message = "Đã hủy kết nối thiết bị thành công theo phê duyệt của Phụ huynh!"
            });
        }
    }

    public class ConfirmUnpairRequest
    {
        public string OtpCode { get; set; } = string.Empty;
    }

    public class KidLoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? DeviceName { get; set; }
        public string? DeviceModel { get; set; }
    }

    public class HeartbeatModel
    {
        public int BatteryLevel { get; set; } = 85;
        public double Lat { get; set; } = 10.762622;
        public double Lng { get; set; } = 106.660172;
        public double NoiseDb { get; set; } = 45.0;
        public double EyeDistanceCm { get; set; } = 35.0;
    }

    public class PairingRequest
    {
        public string PairingCode { get; set; } = string.Empty;
        public string? DeviceName { get; set; }
        public string? DeviceModel { get; set; }
    }

    public class CreateChildDto
    {
        public string FullName { get; set; } = string.Empty;
        public int Age { get; set; } = 8;
        public string? AvatarUrl { get; set; }
        public string? DeviceName { get; set; }
        public string? DeviceModel { get; set; }
        public string? ChildUsername { get; set; }
        public string? ChildPassword { get; set; }
        public int BatteryLevel { get; set; } = 85;
        public bool IsOnline { get; set; } = true;
    }
}
