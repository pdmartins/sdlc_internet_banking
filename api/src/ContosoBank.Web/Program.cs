using ContosoBank.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

var builder = WebApplication.CreateBuilder(args);

// Configure Azure Key Vault (if in production)
if (!builder.Environment.IsDevelopment())
{
    var keyVaultEndpoint = builder.Configuration["AzureKeyVault:VaultUri"];
    if (!string.IsNullOrEmpty(keyVaultEndpoint))
    {
        var secretClient = new SecretClient(new Uri(keyVaultEndpoint), new DefaultAzureCredential());
        builder.Configuration.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
    }
}

// Add services to the container
builder.Services.AddControllers();

// Configure Data Protection for CSRF tokens
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("./keys"))
    .SetApplicationName("ContosoBank");

// Add Antiforgery services for CSRF protection
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.SuppressXFrameOptionsHeader = false;
});

// Configure Azure AD Authentication
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
}

// Add Authorization
builder.Services.AddAuthorization();

// Configure HTTPS Redirection
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
    options.HttpsPort = 443;
});

// Configure Forward Headers for proper HTTPS detection
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000", "https://localhost:3000") // React development server
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10)); // Cache preflight response for 10 minutes
    });
    
    // Add a more permissive policy for development debugging
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    }
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

// Configure Forward Headers (must be early in pipeline)
app.UseForwardedHeaders();

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
    // HSTS (HTTP Strict Transport Security)
    app.UseHsts();
}

// Enhanced Security Headers
app.Use(async (context, next) =>
{
    // Prevent clickjacking
    context.Response.Headers["X-Frame-Options"] = "DENY";
    
    // Prevent MIME type sniffing
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    
    // XSS Protection (for older browsers)
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    
    // Referrer Policy
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    // Content Security Policy
    context.Response.Headers["Content-Security-Policy"] = 
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'";
    
    // Permissions Policy (formerly Feature Policy)
    context.Response.Headers["Permissions-Policy"] = 
        "geolocation=(), microphone=(), camera=()";
    
    await next();
});

// Enable CORS (must be before HTTPS redirection for preflight requests)
app.UseCors("AllowReactApp");

// HTTPS Redirection (enforces HTTPS) - Disabled in development for CORS testing
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

// Add Antiforgery middleware
app.UseAntiforgery();

app.MapControllers();

// Add health check endpoint
app.MapHealthChecks("/health");

// Error handling endpoint
app.Map("/error", () => Results.Problem("An error occurred processing your request"));

app.Run();
