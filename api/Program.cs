using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore; // Add this using directive for EF Core
using InternetBankingAPI.Data; // Add this using directive for the AppDbContext
using InternetBankingAPI.Services; // Add this using directive for the IdentityVerificationService

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Update CORS policy to allow credentials
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // Adjust the origin as needed
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Allow credentials
    });
});

// Add database context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("InternetBankingDb")); // Replace with actual database provider if needed

// Register the IdentityVerificationService
builder.Services.AddScoped<IdentityVerificationService>();
builder.Services.AddScoped<IDocumentVerificationService, DocumentVerificationService>();
builder.Services.AddScoped<IBiometricVerificationService, BiometricVerificationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();

app.UseCors("AllowFrontend");

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.MapControllers(); // Ensure the app is configured to use controllers

app.Run();
