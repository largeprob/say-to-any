using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SqlBoTx.Net.Share.Configuration;
using System.Text;

namespace SqlBoTx.Net.Core.Startups
{
    public static class JwtExtensions
    {
        extension(IServiceCollection services)
        {
            public IServiceCollection AddJwtAuthentication(IConfiguration configuration)
            {
                var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>()!;

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                        ClockSkew = TimeSpan.Zero
                    };
                    options.Events = new JwtBearerEvents
                    {
                        //授权失败全部返回401
                        OnForbidden = async context =>
                        {
                            context.Response.StatusCode = 401;
                            await context.Response.WriteAsJsonAsync("身份未授权");
                        }
                    };
                });

                services.AddAuthorization();

                return services;
            }
        }
    }
}
