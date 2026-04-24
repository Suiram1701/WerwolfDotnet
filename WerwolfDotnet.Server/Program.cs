using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OpenApi;
using OpenTelemetry.Logs;
using WerwolfDotnet;
using WerwolfDotnet.Roles;
using WerwolfDotnet.Server.Authentication;
using WerwolfDotnet.Server.Hubs;
using WerwolfDotnet.Server.OpenApiFilters;
using WerwolfDotnet.Server.Options;
using WerwolfDotnet.Server.Services;
using WerwolfDotnet.Server.Services.Interfaces;
using GameOptions = WerwolfDotnet.Server.Options.GameOptions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

if (Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") is not null)
{
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;

        logging.AddOtlpExporter();
    });
}

builder.Services
    .AddOptions<GameLobbyOptions>()
    .BindConfiguration("GameLobby")
    .ValidateDataAnnotations();
builder.Services
    .AddOptions<GameOptions>()
    .BindConfiguration("Game")
    .ValidateDataAnnotations();

builder.Services
    .AddSingleton<GameManager>()
    .AddSingleton<IGameSessionStore, InMemoryGameSessionStore>()
    .AddSingleton<IGameSettingsStore, InMemoryGameSettingsStore>()
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
    options.AddSecurityDefinition(TokenAuthenticationScheme.SchemeName, new OpenApiSecurityScheme
    { 
        Type = SecuritySchemeType.Http,
        Name = "Authorization",
        Scheme = "bearer",
        BearerFormat = "PlayerToken",
        In = ParameterLocation.Header,
        Description = "A custom format bearer token which can be used to associate a request with a game and player."
    });
    options.OperationFilter<SecurityFilter>();
    
    options.DocumentFilter<ExportAllModelsFilter>();
    options.DocumentFilter<ExportModelFilter>([new[] { typeof(GameState), typeof(Role), typeof(CauseOfDeath), typeof(Fraction) }]);
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

app.MapStaticAssets();
app.MapControllers();
app.MapHub<GameHub>(GameHubPath);
app.MapFallbackToFile("app.html");

app.Run();