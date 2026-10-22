using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Gjubrarko.Api.Auth;
using Gjubrarko.Application;
using Gjubrarko.Application.Common;
using Gjubrarko.Infrastructure;
using Gjubrarko.Infrastructure.Identity;
using Gjubrarko.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddIdentityCore<AppUser>(o =>
    {
        o.User.RequireUniqueEmail = true;
        o.Password.RequiredLength = 8;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequireUppercase = false;
        o.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager();

var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt settings are missing.");
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Section));
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddHttpClient<TurnstileVerifier>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = "name",
        };
    });
builder.Services.AddAuthorization();

// IP addresses are only used here, in memory, and are never stored with a report.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("create-report", ctx =>
    {
        var signedIn = ctx.User.Identity?.IsAuthenticated == true;
        var key = signedIn ? $"user:{ctx.User.FindFirst("sub")?.Value}" : $"ip:{ctx.Connection.RemoteIpAddress}";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = signedIn ? 20 : 5,
            Window = signedIn ? TimeSpan.FromDays(1) : TimeSpan.FromHours(1),
        });
    });
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        $"ip:{ctx.Connection.RemoteIpAddress}",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
    .AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    ExceptionHandler = async ctx =>
    {
        var error = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
        var (status, detail) = error is AppException e ? (e.StatusCode, e.Message) : (500, "Something went wrong.");
        ctx.Response.StatusCode = status;
        await Results.Problem(statusCode: status, detail: detail).ExecuteAsync(ctx);
    },
    // Expected errors (validation, not found) are answered as problem details without logging a stack trace.
    SuppressDiagnosticsCallback = ctx => ctx.Exception is AppException,
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<DataSeeder>()
        .SeedAsync(Path.Combine(app.Environment.ContentRootPath, app.Configuration["SeedFolder"] ?? "../../seed"));
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

app.Run();

public partial class Program;
