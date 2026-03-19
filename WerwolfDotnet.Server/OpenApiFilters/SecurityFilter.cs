using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using WerwolfDotnet.Server.Authentication;

namespace WerwolfDotnet.Server.OpenApiFilters;

public sealed class SecurityFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.GetCustomAttribute<AuthorizeAttribute>() is null)
            return;
        
        (operation.Security ??= new List<OpenApiSecurityRequirement>()).Add(new OpenApiSecurityRequirement
        {
            { new OpenApiSecuritySchemeReference(TokenAuthenticationScheme.SchemeName, context.Document), [] }
        });
        (operation.Responses ??= new OpenApiResponses()).Add("401", new OpenApiResponse
        {
            Description = "You need to be authenticated in order to execute this endpoint."
        });
        operation.Responses.Add("403", new OpenApiResponse
        {
            Description = "You are not authorized to access this endpoint with the given parameters."
        });
    }
}