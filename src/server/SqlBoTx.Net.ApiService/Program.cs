using Microsoft.OpenApi;
using SqlBoTx.Net.ApiService;
using SqlBoTx.Net.Application;
using SqlBoTx.Net.Core.Controller;
using SqlBoTx.Net.Core.ExceptionHandler;
using SqlBoTx.Net.Core.Startups;
using SqlBoTx.Net.Domain;
using SqlBoTx.Net.EFCore;
using SqlBoTx.Net.ServiceDefaults;
using SqlBoTx.Net.Share.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddValidation();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddAuthorization();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<AuthorizationOperationTransformer>();
});
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Say To Any API", Version = "v1" });
});

builder.Services.AddDomainManagers();
builder.AddEFCore();
builder.AddApplicationService();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "http://localhost:5173",
            "http://0.0.0.0:5173",
            "https://0.0.0.0:5173",
            "http://192.168.0.100:5173",
            "https://192.168.0.100:5173",
            "http://localhost:6013",
            "http://127.0.0.1:6013",
            "http://tauri.localhost",
            "https://tauri.localhost"
        };

        policy.SetIsOriginAllowed(origin =>
                allowedOrigins.Contains(origin)
                || origin.StartsWith("tauri://localhost", StringComparison.OrdinalIgnoreCase))
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors();
app.SystemApi();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.MapDefaultEndpoints();
app.MapControllers();
app.Run();
