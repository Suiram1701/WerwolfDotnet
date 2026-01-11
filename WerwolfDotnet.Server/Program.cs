using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.SignalR;
using WerwolfDotnet.Server.Game;
using WerwolfDotnet.Server.Hubs;
using WerwolfDotnet.Server.OpenApiFilters;
using WerwolfDotnet.Server.Options;
using WerwolfDotnet.Server.Services;
using WerwolfDotnet.Server.Services.Interfaces;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<GameLobbyOptions>()
    .BindConfiguration("GameLobby")
    .ValidateDataAnnotations();

builder.Services
    .AddSingleton<GameManager>()
    .AddSingleton<IGameSessionStore, InMemoryGameSessionStore>()
    .AddSingleton<PlayerConnectionMapper>()
    .AddSingleton<IUserIdProvider, TokenAuthenticationUserIdProvider>();

builder.Services
    .AddAuthentication(TokenAuthenticationScheme.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, TokenAuthenticationScheme>(TokenAuthenticationScheme.SchemeName, null);
builder.Services.AddAuthentication();

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSwaggerGen(options =>
{
    options.DocumentFilter<ExportAllModelsFilter>();
    options.DocumentFilter<ExportModelFilter>([new[] { nameof(GameState), nameof(ActionOptions) }]);
    options.SchemaFilter<EnumNamesSchemaFilter>();
    
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

const string websiteClientOrigin = "website_client";
builder.Services.AddCors(options =>
{
    options.AddPolicy(websiteClientOrigin, policy =>
    {
        string? website = builder.Configuration["Endpoints:Website"];
        if (website != null)
        {
            policy.WithOrigins(website)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});
builder.Services.AddProblemDetails();

WebApplication app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseCors(websiteClientOrigin);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<GameHub>(GameHubPath);
app.Run();