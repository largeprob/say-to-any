using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqlBoTx.Net.Domain;
using SqlBoTx.Net.Domain.Auth;
using SqlBoTx.Net.Domain.Organization;
using SqlBoTx.Net.EFCore.Repositorys;

namespace SqlBoTx.Net.EFCore
{
    public static class EFCoreExtensions
    {
        public static IHostApplicationBuilder AddEFCore(this IHostApplicationBuilder builder)
        {
            builder.AddSqlServerDbContext<SqlBotxDBContext>("say2any", configureDbContextOptions: options =>
            {
                options.UseSqlServer(sqlBuilder =>
                {
                    sqlBuilder.EnableRetryOnFailure(0);
                });
            });
            builder.Services.AddEFCoreRepository();
            return builder;
        }

        private static IServiceCollection AddEFCoreRepository(this IServiceCollection services)
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IOrganizationRepository, OrganizationRepository>();
            services.AddScoped<IOrganizationUserRepository, OrganizationUserRepository>();
            services.AddScoped<IOrganizationRoleRepository, OrganizationRoleRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            return services;
        }
    }
}
