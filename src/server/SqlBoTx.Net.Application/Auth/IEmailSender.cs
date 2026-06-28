namespace SqlBoTx.Net.Application.Auth
{
    public interface IEmailSender
    {
        Task SendVerificationCodeAsync(string email, string code, TimeSpan expiresIn);
    }
}
