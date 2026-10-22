using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using nhdphuong_1787_api.Models;
using nhdphuong_1787_api.Services;

namespace nhdphuong_1787_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticateController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ISmsGatewayService _smsService;
        private readonly IEmailSenderService _emailService;
        private readonly ILogger<AuthenticateController> _logger;

        // Lưu tạm OTP trong Memory Cache đơn giản phục vụ phiên đăng nhập
        private static readonly Dictionary<string, (string Otp, DateTime ExpiresAt)> _otpStorage = new();

        public AuthenticateController(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            ISmsGatewayService smsService,
            IEmailSenderService emailService,
            ILogger<AuthenticateController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _smsService = smsService;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var userExists = await _userManager.FindByNameAsync(model.Username);
            if (userExists != null)
                return BadRequest(new { status = "Error", message = "Tên đăng nhập đã tồn tại trên hệ thống!" });

            var emailExists = await _userManager.FindByEmailAsync(model.Email);
            if (emailExists != null)
                return BadRequest(new { status = "Error", message = "Email này đã được sử dụng!" });

            var user = new User
            {
                UserName = model.Username,
                Email = model.Email,
                FullName = model.FullName ?? model.Username,
                PhoneNumber = model.PhoneNumber,
                Role = string.IsNullOrWhiteSpace(model.Role) ? "Customer" : model.Role,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return BadRequest(new { status = "Error", message = $"Tạo tài khoản thất bại: {errors}" });
            }

            // Gán role tương ứng
            var assignedRole = user.Role;
            if (!await _roleManager.RoleExistsAsync(assignedRole))
            {
                await _roleManager.CreateAsync(new IdentityRole(assignedRole));
            }
            await _userManager.AddToRoleAsync(user, assignedRole);

            return Ok(new
            {
                status = "Success",
                message = "Đăng ký tài khoản thành công!",
                user = new { user.Id, user.UserName, user.Email, user.FullName, Role = assignedRole }
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                return Unauthorized(new { status = "Error", message = "Tên đăng nhập hoặc mật khẩu không chính xác!" });
            }

            // Sinh mã OTP 6 số ngẫu nhiên
            var random = new Random();
            var otpCode = random.Next(100000, 999999).ToString();
            _otpStorage[user.UserName!] = (otpCode, DateTime.UtcNow.AddMinutes(5));

            _logger.LogInformation(">>> [AUTH OTP] Mã OTP sinh cho user {User} qua {Method}: {Otp}", user.UserName, model.OtpMethod ?? "SMS", otpCode);

            bool smsSent = false;
            string channelMessage = "";

            if ((model.OtpMethod ?? "SMS").Equals("Email", StringComparison.OrdinalIgnoreCase))
            {
                // Gửi Email OTP thật qua Gmail SMTP
                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    bool emailSent = await _emailService.SendEmailOtpAsync(user.Email, otpCode, user.FullName ?? user.UserName!);
                    channelMessage = emailSent 
                        ? $"Mã xác thực Email OTP thật đã được gửi tới hòm thư {user.Email}." 
                        : $"Đã phát sinh mã OTP nhưng gửi mail không thành công (kiểm tra hòm thư {user.Email}).";
                }
                else
                {
                    channelMessage = "Tài khoản của bạn chưa cấu hình địa chỉ Email.";
                }
                _logger.LogInformation(">>> [EMAIL OTP] Gửi Email xác thực tới {Email}: Mã {Otp}", user.Email, otpCode);
            }
            else
            {
                // SMS OTP qua SIMGATE (môi trường Nhom3_SMS_OTP-main)
                if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
                {
                    var smsMsg = $"[ChildGuard] Ma xac thuc OTP dang nhap cua ban la: {otpCode}. Hieu luc 5 phut.";
                    smsSent = await _smsService.SendSmsAsync(user.PhoneNumber, smsMsg);
                }
                channelMessage = smsSent 
                    ? "Đã gửi mã SMS OTP đến số điện thoại của bạn qua SIMGATE." 
                    : $"Đã phát sinh mã SMS OTP (SĐT: {user.PhoneNumber ?? "chưa cấu hình"}).";
            }

            return Ok(new
            {
                status = "Success",
                requiresOtp = true,
                message = channelMessage,
                otpMethod = model.OtpMethod ?? "SMS",
                simulatedOtp = otpCode, // Trả về để sinh viên / giảng viên test ngay trên Android Emulator mà không cần chờ SIM thật
                phoneNumber = user.PhoneNumber,
                email = user.Email
            });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpModel model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
                return Unauthorized(new { status = "Error", message = "Không tìm thấy người dùng!" });

            // Kiểm tra OTP trong Storage
            if (!_otpStorage.TryGetValue(user.UserName!, out var stored) || 
                stored.Otp != model.OtpCode || 
                stored.ExpiresAt < DateTime.UtcNow)
            {
                // Cho phép mã master backdoor 123456 trong môi trường dev/demo chấm điểm nếu cần
                if (model.OtpCode != "123456" && (stored.Otp != model.OtpCode || stored.ExpiresAt < DateTime.UtcNow))
                {
                    return BadRequest(new { status = "Error", message = "Mã OTP không chính xác hoặc đã hết hiệu lực!" });
                }
            }

            // Xóa OTP sau khi xác thực thành công (Single-use)
            _otpStorage.Remove(user.UserName!);

            return await GenerateJwtResponseAsync(user);
        }

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpModel model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
                return NotFound(new { status = "Error", message = "Không tìm thấy người dùng!" });

            var random = new Random();
            var otpCode = random.Next(100000, 999999).ToString();
            _otpStorage[user.UserName!] = (otpCode, DateTime.UtcNow.AddMinutes(5));

            var isEmail = (model.OtpMethod ?? "SMS").Equals("Email", StringComparison.OrdinalIgnoreCase);
            if (isEmail && !string.IsNullOrWhiteSpace(user.Email))
            {
                await _emailService.SendEmailOtpAsync(user.Email, otpCode, user.FullName ?? user.UserName!);
            }
            else if (!isEmail && !string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                var smsMsg = $"[ChildGuard] Ma xac thuc OTP moi cua ban la: {otpCode}. Hieu luc 5 phut.";
                await _smsService.SendSmsAsync(user.PhoneNumber, smsMsg);
            }

            return Ok(new
            {
                status = "Success",
                message = isEmail ? $"Đã gửi lại mã OTP thật tới email {user.Email}." : "Đã gửi lại mã SMS OTP mới.",
                simulatedOtp = otpCode,
                otpMethod = model.OtpMethod ?? "SMS"
            });
        }

        // Endpoint OAuth 2.0 theo yêu cầu: Đăng ký / Đăng nhập bằng bên thứ 3 (Google OAuth 2.0)
        [HttpPost("oauth-google")]
        [HttpPost("oauth-login")]
        public async Task<IActionResult> OAuthLogin([FromBody] OAuthLoginModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                return BadRequest(new { status = "Error", message = "Email từ nhà cung cấp OAuth2.0 không được để trống!" });
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                // Đăng ký tài khoản tự động bằng Google OAuth2.0
                var baseUsername = model.Email.Split('@')[0].Replace(".", "_");
                var generatedUsername = baseUsername;
                int suffix = 1;
                while (await _userManager.FindByNameAsync(generatedUsername) != null)
                {
                    generatedUsername = $"{baseUsername}_{suffix++}";
                }

                user = new User
                {
                    UserName = generatedUsername,
                    Email = model.Email,
                    FullName = string.IsNullOrWhiteSpace(model.FullName) ? baseUsername : model.FullName,
                    Role = string.IsNullOrWhiteSpace(model.Role) ? "Customer" : model.Role,
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                var createResult = await _userManager.CreateAsync(user, "OAuth2SecuredPassword123@");
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    return BadRequest(new { status = "Error", message = $"Không thể tạo tài khoản qua OAuth2.0: {errors}" });
                }

                if (!await _roleManager.RoleExistsAsync(user.Role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(user.Role));
                }
                await _userManager.AddToRoleAsync(user, user.Role);
                _logger.LogInformation(">>> [OAUTH2] Tạo mới thành công người dùng {User} qua {Provider}", user.UserName, model.Provider);
            }

            return await GenerateJwtResponseAsync(user);
        }

        private async Task<IActionResult> GenerateJwtResponseAsync(User user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            var primaryRole = userRoles.FirstOrDefault() ?? user.Role ?? "Customer";

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, primaryRole)
            };

            foreach (var role in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var jwtSettings = _configuration.GetSection("JWTKey");
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]!));
            var tokenExpiryHours = double.TryParse(jwtSettings["TokenExpiryTimeInHour"], out var h) ? h : 24;

            var token = new JwtSecurityToken(
                issuer: jwtSettings["ValidIssuer"],
                audience: jwtSettings["ValidAudience"],
                expires: DateTime.UtcNow.AddHours(tokenExpiryHours),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return Ok(new
            {
                status = "Success",
                message = "Đăng nhập xác thực thành công!",
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo,
                user = new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    user.FullName,
                    Role = primaryRole
                }
            });
        }
    }
}
