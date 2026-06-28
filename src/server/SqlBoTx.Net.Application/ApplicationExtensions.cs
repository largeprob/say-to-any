using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqlBoTx.Net.Application.Auth;
using SqlBoTx.Net.Application.Contracts.Auth;
using SqlBoTx.Net.Application.Contracts.CopilotVoice;
using SqlBoTx.Net.Application.Contracts.Oss;
using SqlBoTx.Net.Application.CopilotVoice;
using SqlBoTx.Net.Application.Oss;

namespace SqlBoTx.Net.Application
{
    public static class ApplicationExtensions
    {
        public static IHostApplicationBuilder AddApplicationService(this IHostApplicationBuilder builder)
        {
            builder.Services.AddMemoryCache();
            builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));

#pragma warning disable EXTEXP0001
            builder.Services.AddHttpClient("dashscope", client =>
            {
                client.Timeout = TimeSpan.FromMinutes(3);
            })
            .RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001

            builder.Services.AddTransient<ITokenService, TokenService>();
            builder.Services.AddTransient<IAuthService, AuthService>();
            builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddTransient<IAliYunOssService, AliYunOssService>();
            builder.Services.AddTransient<ICopilotVoiceService, CopilotVoiceService>();

            return builder;
        }
    }
}
