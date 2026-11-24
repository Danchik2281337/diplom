using FormaDrive.Data;
using FormaDrive.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormaDrive.Controllers;

public record Reminder(int BookingId, string Text);
public record Inbox(List<Notification> Messages, List<Reminder> Reminders);

[Authorize, Route("notifications")]
public sealed class NotificationsController(FleetDb db, Rentals rentals) : AppController
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var now = rentals.Now;
        var query = db.Bookings.AsNoTracking().Where(b => b.State == TripState.Confirmed || b.State == TripState.Active);
        if (!User.IsInRole("Staff")) query = query.Where(b => b.MemberId == MemberId);
        var upcoming = await query.Where(b => b.State == TripState.Active || b.Start <= now.AddDays(1)).ToListAsync();
        var reminders = new List<Reminder>();
        foreach (var booking in upcoming)
        {
            if (TripSummary.Overdue(booking, now))
                reminders.Add(new(booking.Id, $"Возврат просрочен на {TripSummary.Delay(booking, now)}."));
            else if (booking.State == TripState.Active && booking.End <= now.AddDays(1))
                reminders.Add(new(booking.Id, $"Возврат автомобиля {booking.End:dd.MM HH:mm}, время минское."));
            else if (booking.State == TripState.Confirmed && booking.End > now)
                reminders.Add(new(booking.Id, $"Получение автомобиля {booking.Start:dd.MM HH:mm}, {Tariff.Place(booking.Location)}."));
        }
        var messages = await db.Notifications.Where(n => n.MemberId == MemberId)
            .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id).Take(100).ToListAsync();
        return View(new Inbox(messages, reminders));
    }

    [HttpPost("read")]
    public async Task<IActionResult> Read(int id)
    {
        await db.Notifications.Where(n => n.Id == id && n.MemberId == MemberId && n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, rentals.Now));
        return RedirectToAction(nameof(Index));
    }
}

