using System.Reflection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WerwolfDotnet.Server.OpenApiFilters;

public sealed class ExportModelFilter(string[] arguments) : IDocumentFilter
{
    private readonly string[] _classesToExport = arguments;
    
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var assembly = Assembly.GetExecutingAssembly();
        Type[] modelTypes = assembly
            .GetTypes()
            .Where(t => _classesToExport.Contains(t.Name))
            .ToArray();
        foreach (Type type in modelTypes)
        {
            if (!context.SchemaRepository.Schemas.ContainsKey(type.Name))
                context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
        }
    }
}