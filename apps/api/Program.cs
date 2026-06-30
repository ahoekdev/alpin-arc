using Api.Data;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var seedDemoData = builder.Environment.IsDevelopment();

builder.Services.AddControllers();
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IAuthEmailService, AuthEmailService>();
builder.Services.AddScoped<ILodgeFavoriteService, LodgeFavoriteService>();
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedEmail = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "__Host-AlpinArc.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "__Host-AlpinArc.Xsrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.HeaderName = "X-XSRF-TOKEN";
});

builder.Services.AddScoped<ILodgeService, LodgeService>();
builder.Services.AddScoped<IStageService, StageService>();
builder.Services.AddScoped<ITourService, TourService>();
builder.Services.AddScoped<ITourVariantService, TourVariantService>();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(
    opt => opt.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"))
    .UseSnakeCaseNamingConvention()
    .UseSeeding((context, created) =>
    {
        if (seedDemoData)
        {
            DemoDataSeeder.Seed(context, created);
        }
    })
    .UseAsyncSeeding(async (context, created, cancellationToken) =>
    {
        if (seedDemoData)
        {
            await DemoDataSeeder.SeedAsync(context, created, cancellationToken);
        }
    }));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUi(options =>
    {
        options.DocumentPath = "/openapi/v1.json";
    });

}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
