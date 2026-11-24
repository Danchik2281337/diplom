using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FormaDrive.Data;
using FormaDrive.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace FormaDrive.Controllers;

public record AccountPage(Member Member, List<Booking> Bookings, List<Car> Favorites);
[Route("account")]
public sealed class AccountController(FleetDb db, Rentals rentals) : AppController
{
    private readonly PasswordHasher<Member> hasher = new();

    [HttpGet("login"), HttpGet("register")]
    public IActionResult Access(string? returnUrl) => Form(returnUrl);

    private IActionResult Form(string? returnUrl, string? error = null, string? email = null, string? name = null)
    {
        ViewData["Register"] = Request.Path.Value!.EndsWith("/register", StringComparison.OrdinalIgnoreCase);
        ViewData["Next"] = Url.IsLocalUrl(returnUrl) ? returnUrl : "/account";
        ViewData["Error"] = error;
        ViewData["Email"] = email;
        ViewData["Name"] = name;
        return View("Access");
    }

    [HttpPost("login"), EnableRateLimiting("login")]
    public async Task<IActionResult> Login(string? email, string? password, string? returnUrl)
    {
        var key = email?.Trim().ToLowerInvariant();
        var member = await db.Members.SingleOrDefaultAsync(m => m.Email == key);
        if (member == null || string.IsNullOrEmpty(password) || password.Length > 128 ||
            hasher.VerifyHashedPassword(member, member.PasswordHash, password) == PasswordVerificationResult.Failed)
            return Form(returnUrl, "Проверьте почту и пароль.", email);
        return await SignInMember(member, returnUrl);
    }

    [HttpPost("register"), EnableRateLimiting("login")]
    public async Task<IActionResult> Register(string? name, string? email, string? password, string? returnUrl)
    {
        name = name?.Trim();
        email = email?.Trim().ToLowerInvariant();
        if (name?.Length is null or < 2 or > 80 || email?.Length is null or > 254 || !new EmailAddressAttribute().IsValid(email)
            || password?.Length is null or < 8 or > 128)
            return Form(returnUrl, "Введите имя, корректную почту и пароль длиной от 8 до 128 символов.", email, name);
        var member = new Member { Name = name, Email = email };
        member.PasswordHash = hasher.HashPassword(member, password);
        db.Members.Add(member);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 })
        { return Form(returnUrl, "Эта почта уже зарегистрирована.", email, name); }
        return await SignInMember(member, returnUrl);
    }

    private async Task<IActionResult> SignInMember(Member member, string? next)
    {
        var claims = new[] {new Claim(ClaimTypes.NameIdentifier,member.Id.ToString()),new Claim(ClaimTypes.Name,member.Name),
            new Claim(ClaimTypes.Role,member.Staff ? "Staff" : "Customer")};
        await HttpContext.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        return LocalRedirect(Url.IsLocalUrl(next) ? next! : "/account");
    }

    [Authorize, HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var member = await db.Members.FindAsync(MemberId);
        if (member == null) return await Logout();
        var bookings = await db.Bookings.Include(b => b.Car).Include(b => b.Payments)
            .Where(b => b.MemberId == MemberId).OrderByDescending(b => b.Start).ToListAsync();
        var favorites = await db.Favorites.Where(f => f.MemberId == MemberId).Select(f => f.Car).ToListAsync();
        return View(new AccountPage(member, bookings, favorites));
    }

    [Authorize, HttpPost("profile")]
    public async Task<IActionResult> Profile(string? name)
    {
        name = name?.Trim();
        if (name?.Length is null or < 2 or > 80) TempData["Error"] = "Имя должно содержать от 2 до 80 символов.";
        else
        {
            var user = await db.Members.SingleAsync(m => m.Id == MemberId);
            user.Name = name;
            await db.SaveChangesAsync();
            await SignInMember(user, null);
            TempData["Message"] = "Имя сохранено.";
        }
        return Redirect("/account#profile");
    }

    [Authorize, HttpPost("favorite")]
    public async Task<IActionResult> Favorite(int carId, bool save, string? returnUrl)
    {
        if (!await db.Cars.AnyAsync(c => c.Id == carId)) return NotFound();
        if (save) await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO dbo.Favorites (MemberId, CarId)
            SELECT {MemberId}, {carId}
            WHERE NOT EXISTS (
                SELECT 1 FROM dbo.Favorites WITH (UPDLOCK, HOLDLOCK)
                WHERE MemberId = {MemberId} AND CarId = {carId}
            )
            """);
        else await db.Favorites.Where(f => f.MemberId == MemberId && f.CarId == carId).ExecuteDeleteAsync();
        if (Request.Headers.Accept.ToString().Contains("application/json"))
            return Ok(new { saved = save });
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/account#favorites");
    }

    [Authorize, HttpPost("trips/{id:int}/{actionName:regex(^(cancel|extend)$)}")]
    public async Task<IActionResult> UpdateTrip(int id, string actionName, DateTime? until)
    {
        try
        {
            await rentals.Change(id, MemberId, actionName, until);
            TempData["Message"] = actionName == "extend" ? "Срок и стоимость аренды обновлены." : "Бронь отменена.";
        }
        catch (RentalError e) { TempData["Error"] = e.Message; }
        return Redirect("/account");
    }

    [HttpGet("denied")] public IActionResult Denied() => StatusCode(403, "Доступ только для сотрудников.");
    [Authorize, HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return Redirect("/");
    }
}
