using System.Text;
using Api.Auth;
using Api.Hubs;
using Api.Monitoring;
using Application;
using Application.Monitoring;
using Application.Localization;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Device Detection API",
        Version = "v1",
        Description = "API for ESP32 device detection, exam monitoring, alerts, reporting, and whitelist management."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT access token returned by POST /api/auth/login."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddControllers(options => options.Filters.Add<SessionAuthorizationFilter>());
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var origins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? ["http://localhost:4200", "https://localhost:4200"];

        policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddApplication();
builder.Services.Configure<LocalizationOptions>(
    builder.Configuration.GetSection(LocalizationOptions.SectionName));
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Application.Abstractions.Security.ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<IMonitoringEventPublisher, SignalRMonitoringEventPublisher>();
builder.Services.AddHostedService<ObservationRetentionWorker>();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<DevelopmentUserOptions>(
    builder.Configuration.GetSection(DevelopmentUserOptions.SectionName));
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthBootstrapper>();
builder.Services.AddSingleton<IPasswordHasher<Domain.Entities.AppUser>, PasswordHasher<Domain.Entities.AppUser>>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs/monitoring"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
    await scope.ServiceProvider.GetRequiredService<AuthBootstrapper>().SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Device Detection API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Device Detection API";
    });
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuditMiddleware>();

app.MapControllers();
app.MapHub<MonitoringHub>("/hubs/monitoring");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready", async (AppDbContext dbContext, CancellationToken cancellationToken) =>
    await dbContext.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "ready", database = "connected" })
        : Results.Problem("Database is unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable));

app.Run();
