using System.Reflection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WerwolfDotnet.Server.OpenApiFilters;

public sealed class ExportAllModelsFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var assembly = Assembly.GetExecutingAssembly();
        Type[] modelTypes = assembly
            .GetTypes()
            .Where(t => t.IsClass && (t.Namespace?.StartsWith("WerwolfDotnet.Server.Models") ?? false))
            .Where(t => !t.IsNestedPrivate)     // Indicates the type is compiler generated.
            .ToArray();
        foreach (Type type in modelTypes)
        {
            if (!context.SchemaRepository.Schemas.ContainsKey(type.Name))
                context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
        }
    }
}