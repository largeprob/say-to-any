using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace SqlBoTx.Net.ApiService
{
    internal sealed class AuthorizationOperationTransformer : IOpenApiOperationTransformer
    {
        public Task TransformAsync(
            OpenApiOperation operation,
            OpenApiOperationTransformerContext context,
            CancellationToken cancellationToken)
        {
            // 标了 [AllowAnonymous] 的跳过
            var metadata = context.Description.ActionDescriptor.EndpointMetadata;
            if (metadata.OfType<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>().Any())
                return Task.CompletedTask;

            // 没有 [Authorize] 也跳过
            if (!metadata.OfType<Microsoft.AspNetCore.Authorization.IAuthorizeData>().Any())
                return Task.CompletedTask;


            operation.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = new List<string>()
            });

            return Task.CompletedTask;
        }
    }
}
