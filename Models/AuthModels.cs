using System.ComponentModel.DataAnnotations;

namespace nhdphuong_1787_api.Models
{
    public class RegisterModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Role { get; set; } = "Customer"; // Default to Customer (Parent)
    }

    public class LoginModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        // Tùy chọn kênh gửi OTP: "SMS" hoặc "Email"
        public string? OtpMethod { get; set; } = "SMS";
    }

    public class VerifyOtpModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string OtpCode { get; set; } = string.Empty;
    }

    public class ResendOtpModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        public string? OtpMethod { get; set; } = "SMS";
    }

    public class OAuthLoginModel
    {
        [Required]
        public string Provider { get; set; } = "Google"; // "Google", "Facebook", "OAuth2.0"

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? FullName { get; set; }
        public string? IdToken { get; set; }
        public string? Role { get; set; } = "Customer";
    }
}
