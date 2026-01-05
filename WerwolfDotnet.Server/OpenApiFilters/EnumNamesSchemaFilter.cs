using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WerwolfDotnet.Server.OpenApiFilters;

public sealed class EnumNamesSchemaFilter : ISchemaFilter
{
    private const string _extensionName = "x-enum-varnames";
    
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (!context.Type.IsEnum || schema is not OpenApiSchema schemaEnum)     // Can't edit the interface directly
            return;


        JsonValue[] enumNames = context.Type.GetEnumNames().Select(name => JsonValue.Create(name)).ToArray();
        JsonNode array = JsonSerializer.SerializeToNode(enumNames)!;
        schemaEnum.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        schemaEnum.Extensions[_extensionName] = new JsonNodeExtension(array);
    }
}