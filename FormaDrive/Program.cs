using FormaDrive.Data;
using FormaDrive.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();
builder.Configuration.AddJsonFile(".local/settings.json", optional: true).AddEnvironmentVariables();
var connection = builder.Configuration.GetConnectionString("Fleet")
    ?? @"Server=.\SQLEXPRESS;Database=FormaDrive;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
builder.Services.AddDbContext<FleetDb>(o => o.UseSqlServer(connection));
builder.Services.AddScoped<Rentals>();
builder.Services.AddSingleton<TripDocuments>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
{
    o.LoginPath = "/account/login";
    o.AccessDeniedPath = "/account/denied";
    o.Cookie.Name = "FormaDrive.Session";
    o.ExpireTimeSpan = TimeSpan.FromDays(7);
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));
});
var app = builder.Build();
using (var scope = app.Services.CreateScope())
    await Seed.Run(scope.ServiceProvider.GetRequiredService<FleetDb>(), app.Configuration);
app.UseExceptionHandler("/error");
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
