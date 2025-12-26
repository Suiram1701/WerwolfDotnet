using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using WerwolfDotnet.Server.Hubs;
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
    .AddSingleton<IGameSessionStore, InMemoryGameSessionStore>();

builder.Services
    .AddAuthentication(TokenAuthenticationScheme.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, TokenAuthenticationScheme>(TokenAuthenticationScheme.SchemeName, options =>
    {

    });
builder.Services.AddAuthentication();

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});
builder.Services.AddSignalR();

const string websiteClientOrigin = "website_client";
builder.Services.AddCors(options =>
{
    options.AddPolicy(websiteClientOrigin, policy =>
    {
        string? website = builder.Configuration["Endpoints:Website"];
        if (website != null)
        {
            policy.WithOrigins(website).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
    app.UseHsts();
    app.UseCors(websiteClientOrigin);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<GameHub>(GameHubPath);
app.Run();