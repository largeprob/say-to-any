using Microsoft.Extensions.DependencyInjection;
using SqlBoTx.Net.Domain.Organization;

namespace SqlBoTx.Net.Domain
{
    public static class DomainExtensions
    {
        public static IServiceCollection AddDomainManagers(this IServiceCollection services)
        {
            services.AddScoped<OrganizationManager>();
            services.AddScoped<OrganizationUserManager>();
            services.AddScoped<OrganizationRoleManager>();

            return services;
        }
    }
}
