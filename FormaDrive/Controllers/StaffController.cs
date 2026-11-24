using Dapper;
using FormaDrive.Data;
using FormaDrive.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormaDrive.Controllers;

public sealed class UsageRow { public string Name { get; set; } = ""; public int Trips { get; set; } }
public record Desk(DateTime Week, DateTime Today, List<Car> Cars, List<Booking> Trips, List<ServicePeriod> Service, List<UsageRow> Report);
[Authorize(Roles = "Staff"), Route("staff")]
public sealed class StaffController(FleetDb db, Rentals rentals) : AppController
{
    [HttpGet("")]
    public async Task<IActionResult> Index(DateTime? week)
    {
        var today = rentals.Now.Date;
        var first = week?.Date ?? today;
        if (first.Year is < 2000 or > 2099) first = today;
        var last = first.AddDays(7);
        var month = new DateTime(today.Year, today.Month, 1);
        var trips = await db.Bookings.Include(b => b.Car).Include(b => b.Member)
            .Include(b => b.Payments)
            .Where(b => b.Start < last && b.End > first || b.State == TripState.Active)
            .OrderBy(b => b.Start).ToListAsync();
        var report = await db.Database.GetDbConnection().QueryAsync<UsageRow>("""
            SELECT c.Name, COUNT(b.Id) AS Trips FROM Cars c LEFT JOIN Bookings b
              ON b.CarId=c.Id AND b.[Start] >= @month AND b.[Start] < @next AND b.State <> @cancelled
            GROUP BY c.Id,c.Name ORDER BY Trips DESC,c.Name
            """, new { month, next = month.AddMonths(1), cancelled = (int)TripState.Cancelled });
        return View(new Desk(first, today, await db.Cars.OrderBy(c => c.Id).ToListAsync(), trips,
            await db.ServicePeriods.Where(p => p.Start < last && p.End > first).ToListAsync(), report.ToList()));
    }

    [HttpPost("trips/{id:int}/{actionName:regex(^(issue|return)$)}")]
    [RequestSizeLimit(32 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 32 * 1024 * 1024)]
    public async Task<IActionResult> Inspect(int id, string actionName, int? km, string? notes, DateTime? week,
        string[] zones, List<IFormFile> photos)
    {
        try
        {
            if (!ModelState.IsValid) throw new RentalError("Проверьте пробег и данные осмотра.");
            var inspection = await InspectionUploads.Read(MemberId, notes, zones, photos);
            await rentals.Change(id, null, actionName, km: km, notes: notes, inspection: inspection);
            TempData["Message"] = actionName == "issue" ? "Выдача оформлена." : "Возврат оформлен.";
        }
        catch (RentalError e) { TempData["Error"] = e.Message; }
        return RedirectToAction("Details", "Trips", new { id });
    }

    [HttpPost("trips/{id:int}/payment")]
    public async Task<IActionResult> Payment(int id, PaymentKind kind, int amount, string? note, Guid requestId)
    {
        try
        {
            if (!ModelState.IsValid) throw new RentalError("Проверьте сумму операции.");
            await rentals.RecordPayment(id, MemberId, kind, amount, note, requestId);
            TempData["Message"] = "Операция сохранена.";
        }
        catch (RentalError e) { TempData["Error"] = e.Message; }
        return RedirectToAction("Details", "Trips", new { id });
    }

    [HttpPost("maintenance")]
    public async Task<IActionResult> Maintenance(int carId, DateTime start, DateTime end)
    {
        try { await rentals.Maintenance(carId, start, end); TempData["Message"] = "Время обслуживания зарезервировано."; }
        catch (RentalError e) { TempData["Error"] = e.Message; }
        return RedirectToAction(nameof(Index), new { week = start.Year >= 2000 ? start.ToString("yyyy-MM-dd") : null });
    }

    [HttpPost("rate")]
    public async Task<IActionResult> Rate(int carId, int rate)
    {
        if (rate is < 1 or > 10000 || await db.Cars.Where(c => c.Id == carId).ExecuteUpdateAsync(s => s.SetProperty(c => c.Rate, rate)) != 1)
            TempData["Error"] = "Проверьте автомобиль и тариф от 1 до 10 000 BYN.";
        else TempData["Message"] = "Тариф для новых броней сохранён.";
        return Redirect("/staff");
    }
}
