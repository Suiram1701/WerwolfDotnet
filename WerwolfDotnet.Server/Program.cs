using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OpenApi;
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

builder.Services
    .AddOptions<GameLobbyOptions>()
    .BindConfiguration("GameLobby")
    .ValidateDataAnnotations();
builder.Services
    .AddOptions<GameOptions>()
    .BindConfiguration("GameOptions")
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
    options.AddSecurityDefinition(TokenAuthenticationScheme.SchemeName, new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "A custom format bearer token which can be used to associate a request with a game and player.",
        In = ParameterLocation.Header,
        BearerFormat = TokenAuthenticationScheme.SchemeName
    });
    
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