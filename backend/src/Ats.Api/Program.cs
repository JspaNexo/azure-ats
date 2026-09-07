using Microsoft.EntityFrameworkCore;
using Serilog;
using Ats.Api.Middlewares;
using Ats.Application;
using Ats.Infrastructure;
using Ats.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog Setup
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// 2. Add Layer Services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 3. Add API & Swagger Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// 3.1. Keycloak JWT Bearer Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var authority = builder.Configuration["Authentication:Authority"] ?? "http://localhost:8085/realms/ats-realm";
    var validIssuer = builder.Configuration["Authentication:ValidIssuer"] ?? "http://localhost:8085/realms/ats-realm";

    options.Authority = authority;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuers = new[] { validIssuer, authority, "http://localhost:8085/realms/ats-realm", "http://keycloak:8080/realms/ats-realm" },
        ValidateAudience = false,
        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };

    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            if (context.Principal?.Identity is System.Security.Claims.ClaimsIdentity identity)
            {
                var realmAccessClaim = identity.FindFirst("realm_access");
                if (realmAccessClaim != null)
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(realmAccessClaim.Value);
                        if (doc.RootElement.TryGetProperty("roles", out var rolesElement) && rolesElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            foreach (var role in rolesElement.EnumerateArray())
                            {
                                var roleName = role.GetString();
                                if (!string.IsNullOrEmpty(roleName))
                                {
                                    identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, roleName));
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("ats_admin"));
    options.AddPolicy("RequireRecruiterOrAdmin", policy => policy.RequireRole("ats_admin", "ats_recruiter"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// 4. Auto-migration / ensure DB schema created on startup (Dev setup)
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
        logger.LogInformation("Esquema de base de datos verificado y listo.");
    }
    catch (Exception ex)
    {
        logger.LogWarning("No se pudo conectar a PostgreSQL en el arranque: {Message}", ex.Message);
    }
}

// 5. Middleware Pipeline
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Make Program accessible for WebApplicationFactory in integration tests
public partial class Program { }
