WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

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

app.MapControllers();
app.Run();