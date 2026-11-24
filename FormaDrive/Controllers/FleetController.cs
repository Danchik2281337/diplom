using System.Security.Claims;
using FormaDrive.Data;
using FormaDrive.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormaDrive.Controllers;

public record Search(DateTime? Start = null, DateTime? End = null, string Location = Tariff.Office,
    string Tab = "all", string? Gear = null, int? Seats = null, string? Purpose = null);
public record Catalog(List<Car> Cars, Search Search);
public record VehiclePage(Car Car, DateTime Start, DateTime End, string Location, bool Favorite, bool Seat);
public abstract class AppController : Controller
{
    protected int MemberId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public sealed class FleetController(FleetDb db, Rentals rentals) : AppController
{
    [HttpGet("/")]
    public async Task<IActionResult> Index([FromQuery] Search search)
    {
        IQueryable<Car> query = db.Cars.AsNoTracking();
        if (!ModelState.IsValid || search.Start.HasValue || search.End.HasValue)
        {
            try
            {
                if (!ModelState.IsValid || search.Start is null || search.End is null) throw new RentalError("Укажите даты и время получения и возврата.");
                Tariff.Validate(search.Start.Value, search.End.Value, search.Location, rentals.Now);
                query = rentals.Available(search.Start.Value, search.End.Value);
            }
            catch (RentalError e) { ViewData["Error"] = e.Message; query = query.Where(c => false); }
        }
        if (search.Tab == "premium") query = query.Where(c => c.Premium);
        if (search.Tab == "city") query = query.Where(c => !c.Premium);
        if (search.Gear is "Автомат" or "Механика") query = query.Where(c => c.Transmission == search.Gear);
        if (search.Seats is > 0) query = query.Where(c => c.Seats >= search.Seats);
        query = search.Purpose switch
        {
            "city" => query.Where(c => c.Body == "Хэтчбек"),
            "family" => query.Where(c => c.Seats >= 5 && c.Body == "Кроссовер"),
            "weekend" => query.Where(c => c.Transmission == "Автомат" && !c.Premium),
            "occasion" => query.Where(c => c.Premium),
            _ => query
        };
        return View(new Catalog(await query.OrderBy(c => c.Id).ToListAsync(), search));
    }

    [HttpGet("/compare")]
    public async Task<IActionResult> Compare(int[] ids, DateTime? start, DateTime? end, string location = Tariff.Office)
    {
        var chosen = ids.Distinct().Take(3).ToArray();
        var cars = await db.Cars.AsNoTracking().Where(c => chosen.Contains(c.Id)).OrderBy(c => c.Id).ToListAsync();
        var available = new HashSet<int>(cars.Select(c => c.Id));
        if (start.HasValue || end.HasValue)
        {
            try
            {
                if (!ModelState.IsValid || start == null || end == null) throw new RentalError("Укажите обе даты.");
                Tariff.Validate(start.Value, end.Value, location, rentals.Now);
                available = (await rentals.Available(start.Value, end.Value).Select(c => c.Id).ToListAsync()).ToHashSet();
            }
            catch (RentalError error) { ViewData["Error"] = error.Message; available.Clear(); }
        }
        return View(new Comparison(cars, start, end, location, available));
    }

    [HttpGet("/api/cars/{id:int}/schedule")]
    public async Task<IActionResult> Schedule(int id, DateTime? month)
    {
        Response.Headers.CacheControl = "no-store";
        var first = month ?? rentals.Now;
        if (!ModelState.IsValid || first.Year is < 2000 or > 2098) return BadRequest();
        first = new DateTime(first.Year, first.Month, 1);
        var end = first.AddMonths(1);
        var now = rentals.Now;
        var bookings = await db.Bookings.AsNoTracking()
            .Where(b => b.CarId == id && (b.State == TripState.Confirmed || b.State == TripState.Active)
                && b.Start < end && (b.End > first || b.State == TripState.Active && b.End < now))
            .Select(b => new { b.Start, End = b.State == TripState.Active && b.End < now ? DateTime.MaxValue : b.End }).ToListAsync();
        var service = await db.ServicePeriods.AsNoTracking()
            .Where(p => p.CarId == id && p.Start < end && p.End > first)
            .Select(p => new { p.Start, p.End }).ToListAsync();
        // Return only occupied intervals, without customer details or booking numbers.
        return Ok(new { month = first.ToString("yyyy-MM-dd"), intervals = bookings.Concat(service)
            .Select(p => new { start = p.Start.ToString("yyyy-MM-ddTHH:mm"), end = p.End.ToString("yyyy-MM-ddTHH:mm") }) });
    }

    [HttpGet("/cars/{id:int}")]
    public async Task<IActionResult> Details(int id, DateTime? start, DateTime? end, string location = Tariff.Office, bool seat = false)
    {
        var car = await db.Cars.FindAsync(id);
        if (car == null) return NotFound();
        var from = start ?? rentals.Now.Date.AddDays(1).AddHours(10);
        bool favorite = User.Identity!.IsAuthenticated && await db.Favorites.AnyAsync(f => f.CarId == id && f.MemberId == MemberId);
        return View(new VehiclePage(car, from, end ?? from.AddDays(2), location, favorite, seat));
    }

    [HttpGet("/api/cars/{id:int}/quote")]
    public async Task<IActionResult> Quote(int id, DateTime start, DateTime end, string location, bool seat)
    {
        Response.Headers.CacheControl = "no-store";
        if (!ModelState.IsValid) return BadRequest(new { message = "Проверьте даты аренды." });
        try { Tariff.Validate(start, end, location, rentals.Now); }
        catch (RentalError e) { return BadRequest(new { message = e.Message }); }
        var car = await rentals.Available(start, end).AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
        if (car == null) return Conflict(new { message = "Автомобиль занят на выбранные даты." });
        int days = Tariff.Days(start, end), extras = Tariff.Extras(location, seat);
        return Ok(new { days, rate = car.Rate, extras, total = days * car.Rate + extras });
    }

    [Authorize, HttpPost("/cars/{id:int}/book")]
    public async Task<IActionResult> Book(int id, DateTime start, DateTime end, string location, bool seat, int expected)
    {
        try
        {
            if (!ModelState.IsValid) throw new RentalError("Проверьте параметры аренды.");
            var number = await rentals.Book(MemberId, id, start, end, location, seat, expected);
            TempData["Message"] = $"Бронь №{number} подтверждена. Оплата при получении.";
            TempData["BookingId"] = number;
            return Redirect("/account");
        }
        catch (RentalError e)
        {
            TempData["Error"] = e.Message;
            return RedirectToAction(nameof(Details), new { id, start = start.ToString("s"), end = end.ToString("s"), location, seat });
        }
    }

    [HttpGet("/error"), HttpPost("/error"), IgnoreAntiforgeryToken]
    public IActionResult ErrorPage() => Problem(statusCode: 500, title: "Не удалось выполнить запрос. Попробуйте ещё раз.");
}
