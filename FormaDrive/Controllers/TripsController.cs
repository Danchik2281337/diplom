using FormaDrive.Data;
using FormaDrive.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormaDrive.Controllers;

public record TripPage(Booking Booking, Booking? NextTrip, bool Staff);

[Authorize, Route("trips")]
public sealed class TripsController(FleetDb db, Rentals rentals, TripDocuments documents) : AppController
{
    private IQueryable<Booking> VisibleTrips()
    {
        var query = db.Bookings.AsNoTracking().AsSplitQuery();
        if (!User.IsInRole("Staff")) query = query.Where(b => b.MemberId == MemberId);
        return query;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, bool panel = false)
    {
        var trip = await VisibleTrips().Include(b => b.Car).Include(b => b.Member).Include(b => b.Payments)
            .Include(b => b.Inspections).SingleOrDefaultAsync(b => b.Id == id);
        if (trip == null) return NotFound();

        // Load only photo metadata on screens, the original files have a separate protected endpoint.
        foreach (var inspection in trip.Inspections)
            inspection.Photos = await db.InspectionPhotos.Where(p => p.InspectionId == inspection.Id)
                .Select(p => new InspectionPhoto { Id = p.Id, InspectionId = p.InspectionId, ContentType = p.ContentType, Content = Array.Empty<byte>() })
                .ToListAsync();

        Booking? next = null;
        if (User.IsInRole("Staff"))
            next = await db.Bookings.AsNoTracking()
                .Where(b => b.CarId == trip.CarId && b.Id != id && b.State == TripState.Confirmed && b.Start >= trip.End)
                .OrderBy(b => b.Start).FirstOrDefaultAsync();
        var model = new TripPage(trip, next, User.IsInRole("Staff"));
        return panel ? PartialView("_Trip", model) : View(model);
    }

    [HttpGet("photos/{id:int}")]
    public async Task<IActionResult> Photo(int id)
    {
        var query = db.InspectionPhotos.AsNoTracking().Where(p => p.Id == id);
        if (!User.IsInRole("Staff"))
            query = query.Where(p => p.Inspection.Booking.MemberId == MemberId);
        var photo = await query.SingleOrDefaultAsync();
        if (photo == null) return NotFound();
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(photo.Content, photo.ContentType);
    }

    [HttpGet("{id:int}/document/{stage:regex(^(issue|return)$)}")]
    public async Task<IActionResult> Document(int id, string stage)
    {
        var trip = await VisibleTrips().Include(b => b.Car).Include(b => b.Member).Include(b => b.Payments)
            .Include(b => b.Inspections).ThenInclude(i => i.Photos).SingleOrDefaultAsync(b => b.Id == id);
        if (trip == null) return NotFound();
        bool returned = stage == "return";
        if (returned ? !trip.InKm.HasValue : !trip.OutKm.HasValue) return NotFound();
        Response.Headers.CacheControl = "private, no-store";
        return File(documents.Create(trip, returned), "application/pdf", $"FormaDrive-{id}-{stage}.pdf");
    }
}

