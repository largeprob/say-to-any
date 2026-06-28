using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using SqlBoTx.Net.Domain.Organization;
using SqlBoTx.Net.EFCore;
using SqlBoTx.Net.Share.Helpers;

namespace SqlBoTx.Net.DbManager
{
    public class DbInitializer(
        IServiceProvider serviceProvider,
        ILogger<DbInitializer> logger,
        IHostApplicationLifetime hostApplicationLifetime) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqlBotxDBContext>();
            await InitializeDatabaseAsync(dbContext, cancellationToken);
        }

        public async Task InitializeDatabaseAsync(SqlBotxDBContext dbContext, CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();

            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(dbContext.Database.MigrateAsync, cancellationToken);

            logger.LogInformation("数据库迁移完成，花费 {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
            await SeedUsersAsync(dbContext, cancellationToken);
            logger.LogInformation("种子数据完成");
        }

        private static async Task SeedUsersAsync(SqlBotxDBContext dbContext, CancellationToken cancellationToken)
        {
            if (await dbContext.Set<OrganizationUser>().AnyAsync(cancellationToken))
            {
                return;
            }

            var defaultRole = new OrganizationRole
            {
                Name = "系统账号",
                Description = "系统账号",
                SortOrder = 0,
                IsActive = true,
            };

            var salt = SecurityHelper.GenerateSalt();
            var hashedPassword = SecurityHelper.HashStr("admin", salt) + "|" + salt;

            var organization = new Organization
            {
                Name = "Say To Any",
                Description = "Say To Any 默认组织",
                Users = new List<OrganizationUser>
                {
                    new()
                    {
                        UserName = "系统账号",
                        LoginAccount = "admin",
                        Password = hashedPassword,
                        Admin = true,
                        OrganizationRole = defaultRole,
                    }
                }
            };

            await dbContext.AddAsync(organization, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
