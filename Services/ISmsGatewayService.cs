namespace nhdphuong_1787_api.Services
{
    public interface ISmsGatewayService
    {
        Task<bool> SendSmsAsync(string phoneNumber, string message);
        string NormalizePhone(string phone);
    }
}
