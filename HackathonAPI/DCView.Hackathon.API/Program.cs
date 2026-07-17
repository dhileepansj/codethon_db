using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using DCView.Hackathon.API.Middleware;
using DCView.Hackathon.Application.Extensions;
using DCView.Hackathon.Infrastructure.Data;
using DCView.Hackathon.Infrastructure.Extensions;
using DCView.Hackathon.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Allow Npgsql to handle DateTime with Kind=Unspecified by treating them as UTC
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Configuration
var env = builder.Environment.EnvironmentName;
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// JWT
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new Exception("JWT Key is missing in appsettings.json");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"]
        };
    });

builder.Services.AddAuthorization();

// EF Core + Repositories
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddRepositories();

// Application Services
builder.Services.AddApplicationLayer();

// HttpClient for Ollama AI detection
builder.Services.AddHttpClient("Ollama", client =>
{
    client.Timeout = TimeSpan.FromSeconds(120);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

// Controllers + Response Wrapper
builder.Services
    .AddControllers()
    .AddResponseWrapper();

builder.Services.AddEndpointsApiExplorer();

// Swagger with JWT
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SQL Hackathon API",
        Version = "v1",
        Description = "API for SQL Hackathon Platform"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Apply migrations at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HackathonDbContext>();
    db.Database.Migrate();
}

// Middleware pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Enable Swagger in Production too (remove this block if not needed)
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UsePathBase("/hackathonapi");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<SessionValidationMiddleware>();
app.MapControllers();

app.Run();
