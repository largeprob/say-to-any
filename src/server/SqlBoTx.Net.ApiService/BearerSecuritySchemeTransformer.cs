using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace SqlBoTx.Net.ApiService
{
    internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider)
        : IOpenApiDocumentTransformer
    {
        public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();

            // 只有启用了 Bearer 认证才添加 Security Scheme
            if (authenticationSchemes.Any(authScheme => authScheme.Name == JwtBearerDefaults.AuthenticationScheme))
            {
                var securitySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    ["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",          // 必须小写，OpenAPI 规范要求
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Description = "请输入 JWT Token，不需要加 'Bearer ' 前缀"
                    }
                };

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = securitySchemes;

                // 可选：全局为所有接口添加安全要求（所有接口默认需要 JWT Token）
                // foreach (var path in document.Paths.Values)
                // {
                //     foreach (var operation in path.Operations.Values)
                //     {
                //         operation.Security ??= new List<OpenApiSecurityRequirement>();
                //         operation.Security.Add(new OpenApiSecurityRequirement
                //         {
                //             [new OpenApiSecuritySchemeReference("Bearer", document)] = Array.Empty<string>()
                //         });
                //     }
                // }
            }
        }
    }
}
