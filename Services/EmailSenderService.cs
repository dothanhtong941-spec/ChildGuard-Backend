using System.Net;
using System.Net.Mail;

namespace nhdphuong_1787_api.Services
{
    public class EmailSenderService : IEmailSenderService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSenderService> _logger;

        public EmailSenderService(IConfiguration configuration, ILogger<EmailSenderService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailOtpAsync(string toEmail, string otpCode, string recipientName = "")
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("Email người nhận không hợp lệ để gửi OTP.");
                return false;
            }

            var smtp = _configuration.GetSection("SmtpSettings");
            var server = smtp["Server"] ?? "smtp.gmail.com";
            var port = int.TryParse(smtp["Port"], out var p) ? p : 587;
            var senderEmail = smtp["SenderEmail"] ?? "dothanhtong941@gmail.com";
            var senderPassword = smtp["SenderPassword"] ?? "wvqjpouwujffrgkq";
            var senderName = smtp["SenderName"] ?? "Child Phone Guard Security";

            try
            {
                using var client = new SmtpClient(server, port)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(senderEmail.Trim(), senderPassword.Replace(" ", "").Trim())
                };

                var displayName = string.IsNullOrWhiteSpace(recipientName) ? toEmail.Split('@')[0] : recipientName;
                var mail = new MailMessage
                {
                    From = new MailAddress(senderEmail.Trim(), senderName),
                    Subject = $"[ChildGuard] Mã xác thực OTP đăng nhập: {otpCode}",
                    Body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
</head>
<body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; padding: 30px 10px; margin: 0;'>
    <div style='max-width: 520px; margin: 0 auto; background: #ffffff; border-radius: 16px; padding: 32px; box-shadow: 0 4px 12px rgba(0,0,0,0.06); border: 1px solid #e2e8f0;'>
        <div style='text-align: center; margin-bottom: 24px;'>
            <h2 style='color: #4338ca; margin: 0; font-size: 24px;'>🛡️ Child Phone Guard</h2>
            <p style='color: #64748b; font-size: 13px; margin: 6px 0 0 0;'>Hệ Thống Quản Lý & Bảo Vệ Điện Thoại Trẻ Em</p>
        </div>
        <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 20px 0;'>
        <p style='color: #1e293b; font-size: 15px; line-height: 1.5;'>Xin chào <b>{displayName}</b>,</p>
        <p style='color: #475569; font-size: 14px; line-height: 1.5;'>Bạn (hoặc ai đó) vừa yêu cầu đăng nhập vào tài khoản trên ứng dụng. Mã xác thực 2 bước (OTP) của bạn là:</p>
        <div style='background: #eef2ff; border-radius: 12px; padding: 20px; text-align: center; margin: 24px 0; border: 1px dashed #6366f1;'>
            <span style='font-size: 34px; font-weight: 800; letter-spacing: 10px; color: #3730a3; font-family: monospace;'>{otpCode}</span>
        </div>
        <p style='color: #dc2626; font-size: 13px; font-weight: 600; margin: 0 0 8px 0;'>⏱️ Mã OTP này có hiệu lực trong vòng 5 phút.</p>
        <p style='color: #64748b; font-size: 12px; line-height: 1.5; margin: 0;'>Vì lý do an toàn, tuyệt đối không chia sẻ mã này cho bất kỳ ai khác.</p>
        <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 28px 0 16px 0;'>
        <p style='color: #94a3b8; font-size: 11px; text-align: center; margin: 0;'>© 2026 Child Phone Guard System. All rights reserved.</p>
    </div>
</body>
</html>",
                    IsBodyHtml = true
                };

                mail.To.Add(toEmail.Trim());

                _logger.LogInformation(">>> [SMTP] Đang gửi Email OTP thật tới {Email} qua {Sender}...", toEmail, senderEmail);
                await client.SendMailAsync(mail);
                _logger.LogInformation(">>> [SMTP] Gửi Email OTP thật thành công tới {Email}!", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ">>> [SMTP ERROR] Lỗi khi gửi Email OTP tới {Email}: {Msg}", toEmail, ex.Message);
                return false;
            }
        }
    }
}
