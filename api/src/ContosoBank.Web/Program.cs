using ContosoBank.Infrastructure;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000", "https://localhost:3000") // React development server
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Add Infrastructure services (Database, Repositories, Application Services)
builder.Services.AddInfrastructure(builder.Configuration);

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Contoso Bank API",
        Version = "v1",
        Description = "Internet Banking API for Contoso Bank",
        Contact = new OpenApiContact
        {
            Name = "Contoso Bank Development Team",
            Email = "dev@contosobank.com"
        }
    });

    // Include XML comments for better API documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ContosoBank.Infrastructure.Data.ContosoBankDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Contoso Bank API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at the app's root
    });
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowReactApp");

// Note: Using custom session-based authentication instead of built-in Authorization middleware
// app.UseAuthentication();
// app.UseAuthorization();

app.MapControllers();

// Add health check endpoint
app.MapHealthChecks("/health");

// Error handling endpoint
app.Map("/error", () => Results.Problem("An error occurred processing your request"));

app.Run();
