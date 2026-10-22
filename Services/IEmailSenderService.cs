namespace nhdphuong_1787_api.Services
{
    public interface IEmailSenderService
    {
        Task<bool> SendEmailOtpAsync(string toEmail, string otpCode, string recipientName = "");
    }
}
