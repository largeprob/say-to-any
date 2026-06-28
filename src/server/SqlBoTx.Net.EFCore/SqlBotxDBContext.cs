using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlBoTx.Net.Domain.Auth;
using SqlBoTx.Net.Domain.Organization;

namespace SqlBoTx.Net.EFCore
{
    public class SqlBotxDBContext : ApplicationDbContextBase
    {
        private readonly ILogger<SqlBotxDBContext> _logger;

        public SqlBotxDBContext(DbContextOptions contextOptions, IConfiguration configuration, ILogger<SqlBotxDBContext> logger)
            : base(contextOptions, configuration)
        {
            _logger = logger;
        }

        public DbSet<Organization> Organization { get; set; }
        public DbSet<OrganizationUser> OrganizationUser { get; set; }
        public DbSet<OrganizationRole> OrganizationRole { get; set; }
        public DbSet<RefreshToken> RefreshToken { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entityTypes = GetType().GetProperties()
                .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .Select(p => p.PropertyType.GetGenericArguments()[0])
                .ToList();

            foreach (var entityType in entityTypes)
            {
                EntityTypeBuilder? typeBuilder = modelBuilder.Entity(entityType);
                try
                {
                    BuilderTable(typeBuilder, entityType);
                    BuilderProperty(typeBuilder, entityType);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "EF模型映射失败：{EntityType}", entityType.Name);
                    throw;
                }
            }

            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
