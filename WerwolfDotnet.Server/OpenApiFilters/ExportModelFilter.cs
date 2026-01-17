using System.Reflection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WerwolfDotnet.Server.OpenApiFilters;

public sealed class ExportModelFilter(Type[] arguments) : IDocumentFilter
{
    
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        foreach (Type type in arguments)
        {
            if (!context.SchemaRepository.Schemas.ContainsKey(type.Name))
                context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
        }
    }
}