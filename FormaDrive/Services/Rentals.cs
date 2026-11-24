using System.Data;
using FormaDrive.Data;
using Microsoft.EntityFrameworkCore;

namespace FormaDrive.Services;

public sealed class RentalError(string message) : Exception(message);

public static class Tariff
{
    public const string Office = "office", Airport = "airport";
    public static string Place(string location) => location == Airport ? "Аэропорт Минск" : "Офис в Минске";
    public static string Status(TripState state) => state switch
    {
        TripState.Confirmed => "Подтверждена",
        TripState.Active => "Машина выдана",
        TripState.Finished => "Завершена",
        _ => "Отменена"
    };
    public static int Days(DateTime start, DateTime end) => (int)Math.Ceiling((end - start).TotalDays);
    public static int Extras(string location, bool seat) => (location == Airport ? 40 : 0) + (seat ? 10 : 0);
    public static void Validate(DateTime start, DateTime end, string location, DateTime now)
    {
        if (location is not (Office or Airport)) throw new RentalError("Выберите пункт выдачи.");
        if (start < now || end <= start || end - start > TimeSpan.FromDays(90))
            throw new RentalError("Проверьте даты. Получение должно быть в будущем, срок аренды — до 90 суток.");
    }
}

public sealed partial class Rentals(FleetDb db, TimeProvider clock)
{
    public DateTime Now => clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime;

    public IQueryable<Car> Available(DateTime start, DateTime end, int? excludeBooking = null)
    {
        var now = Now;
        return db.Cars.Where(c =>
        !db.Bookings.Any(b => b.CarId == c.Id && b.Id != excludeBooking && (b.State == TripState.Confirmed || b.State == TripState.Active)
            && b.Start < end && (b.End > start || b.State == TripState.Active && b.End < now)) &&
        !db.ServicePeriods.Any(p => p.CarId == c.Id && p.Start < end && p.End > start));
    }

    // All schedule changes lock this car until their transaction commits.
    private async Task<Car> LockCar(int id)
    {
        var cars = await db.Cars.FromSqlInterpolated($"SELECT * FROM dbo.Cars WITH (UPDLOCK, HOLDLOCK) WHERE Id = {id}").ToListAsync();
        return cars.SingleOrDefault() ?? throw new RentalError("Автомобиль не найден.");
    }

    public async Task<int> Book(int member, int carId, DateTime start, DateTime end, string location, bool seat, int expected)
    {
        Tariff.Validate(start, end, location, Now);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var car = await LockCar(carId);
        if (!await Available(start, end).AnyAsync(c => c.Id == carId))
            throw new RentalError("Машина уже занята на эти даты. Выберите другой автомобиль или период.");
        int extras = Tariff.Extras(location, seat);
        int total = car.Rate * Tariff.Days(start, end) + extras;
        if (expected != total) throw new RentalError("Стоимость изменилась. Проверьте новый расчёт и подтвердите бронь ещё раз.");
        var booking = new Booking
        {
            CarId = carId,
            MemberId = member,
            Start = start,
            End = end,
            Location = location,
            Seat = seat,
            DailyRate = car.Rate,
            Deposit = car.Deposit,
            Extras = extras,
            Total = total
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        await Notify(booking, "Бронь подтверждена. Оплата при получении.", notifyStaff: true);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return booking.Id;
    }

    public async Task Change(int id, int? owner, string action, DateTime? until = null, int? km = null,
        string? notes = null, Inspection? inspection = null)
    {
        var lookup = db.Bookings.Where(b => b.Id == id);
        if (owner.HasValue) lookup = lookup.Where(b => b.MemberId == owner.Value);
        var carId = await lookup.Select(b => (int?)b.CarId).SingleOrDefaultAsync() ?? throw new RentalError("Бронь не найдена.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var car = await LockCar(carId);
        var booking = await lookup.SingleAsync();
        var now = Now;
        switch (action)
        {
            case "cancel" when owner.HasValue && booking.State == TripState.Confirmed && booking.Start > now:
                booking.State = TripState.Cancelled;
                break;
            case "extend" when owner.HasValue && booking.State is TripState.Confirmed or TripState.Active:
                if (until is null || until <= booking.End || until <= now || until - booking.Start > TimeSpan.FromDays(90))
                    throw new RentalError("Выберите более позднее время возврата в пределах 90 суток от начала аренды.");
                if (!await Available(booking.End, until.Value, booking.Id).AnyAsync(c => c.Id == carId))
                    throw new RentalError("Продление недоступно. На это время есть бронь или обслуживание.");
                booking.End = until.Value;
                booking.Total = booking.DailyRate * Tariff.Days(booking.Start, booking.End) + booking.Extras;
                break;
            case "issue" when !owner.HasValue && booking.State == TripState.Confirmed && now >= booking.Start && now < booking.End:
                if (await db.Bookings.AnyAsync(b => b.CarId == carId && b.State == TripState.Active))
                    throw new RentalError("Предыдущая поездка ещё не закрыта.");
                CheckOdometer(car, km, notes);
                booking.OutKm = car.Mileage;
                booking.OutNotes = notes;
                booking.IssuedAt = now;
                booking.State = TripState.Active;
                break;
            case "return" when !owner.HasValue && booking.State == TripState.Active:
                CheckOdometer(car, km, notes);
                booking.InKm = car.Mileage;
                booking.InNotes = notes;
                booking.ReturnedAt = now;
                booking.State = TripState.Finished;
                break;
            default: throw new RentalError("Действие недоступно для этой брони или выбранного времени.");
        }
        if (inspection != null)
        {
            inspection.BookingId = booking.Id;
            inspection.IsReturn = action == "return";
            inspection.CreatedAt = now;
            inspection.Mileage = car.Mileage;
            db.Inspections.Add(inspection);
        }
        var message = action switch
        {
            "cancel" => "Бронь отменена.",
            "extend" => $"Срок аренды продлён до {booking.End:dd.MM.yyyy HH:mm}. Стоимость {booking.Total} BYN.",
            "issue" => "Автомобиль выдан. Приятной поездки!",
            _ => "Возврат автомобиля оформлен. Поездка завершена."
        };
        await Notify(booking, message, notifyStaff: action is "cancel" or "extend");
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private static void CheckOdometer(Car car, int? km, string? notes)
    {
        if (km is null || km < car.Mileage || string.IsNullOrWhiteSpace(notes) || notes.Length > 500)
            throw new RentalError("Проверьте пробег и описание осмотра (до 500 символов).");
        car.Mileage = km.Value;
    }

    public async Task Maintenance(int carId, DateTime start, DateTime end)
    {
        Tariff.Validate(start, end, Tariff.Office, Now);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await LockCar(carId);
        if (!await Available(start, end).AnyAsync(c => c.Id == carId)) throw new RentalError("Выбранный период уже занят.");
        db.ServicePeriods.Add(new() { CarId = carId, Start = start, End = end });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
