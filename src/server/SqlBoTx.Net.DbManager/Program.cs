
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using SqlBoTx.Net.EFCore;
using SqlBoTx.Net.ServiceDefaults;

namespace SqlBoTx.Net.DbManager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.AddServiceDefaults();

            builder.AddSqlServerDbContext<SqlBotxDBContext>("say2any", configureDbContextOptions: options =>
            {
                options.UseSqlServer(sqlBuilder =>
                {
                    sqlBuilder.MigrationsAssembly(typeof(Program).Assembly.GetName().Name);
                });
            });

            builder.Services.AddSingleton<DbInitializer>();

            builder.Services.AddHostedService(sp => sp.GetRequiredService<DbInitializer>());

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("Default", new OpenApiInfo
                {
                    Version = "1.0",
                    Title = "数据库迁移工具"
                });
            });

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();

                app.MapPost("/reset-db", async (SqlBotxDBContext dbContext, DbInitializer dbInitializer, CancellationToken cancellationToken) =>
                {
                    await dbContext.Database.EnsureDeletedAsync(cancellationToken);
                    await dbInitializer.InitializeDatabaseAsync(dbContext, cancellationToken);
                })
                .WithSummary("重置数据库")
                .Produces(500)
                .Produces(200);

                app.MapOpenApi("/openapi.json");
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/openapi.json", "v1");
                });
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
