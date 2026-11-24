using System.Data;
using FormaDrive.Data;
using Microsoft.EntityFrameworkCore;

namespace FormaDrive.Services;

public sealed partial class Rentals
{
    private async Task Notify(Booking booking, string text, bool notifyStaff = false)
    {
        var recipients = new List<int> { booking.MemberId };
        if (notifyStaff)
            recipients.AddRange(await db.Members.Where(m => m.Staff).Select(m => m.Id).ToListAsync());

        foreach (var member in recipients.Distinct())
            db.Notifications.Add(new Notification
            {
                MemberId = member, BookingId = booking.Id,
                Text = $"Бронь №{booking.Id}. {text}", CreatedAt = Now
            });
    }

    public async Task RecordPayment(int id, int staffId, PaymentKind kind, int amount, string? note, Guid requestId)
    {
        if (requestId == Guid.Empty) throw new RentalError("Обновите страницу перед сохранением операции.");
        if (!Enum.IsDefined(kind) || amount <= 0 || amount > 1_000_000 || note?.Length > 500)
            throw new RentalError("Укажите корректную сумму и примечание до 500 символов.");
        if (kind == PaymentKind.DepositWithheld && string.IsNullOrWhiteSpace(note))
            throw new RentalError("Укажите причину удержания из залога.");

        int carId = await db.Bookings.Where(b => b.Id == id).Select(b => b.CarId).SingleOrDefaultAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await LockCar(carId);
        if (await db.Payments.AnyAsync(p => p.RequestId == requestId && p.BookingId == id && p.StaffId == staffId))
            return;
        var booking = await db.Bookings.Include(b => b.Payments).SingleAsync(b => b.Id == id);

        int allowed = kind switch
        {
            PaymentKind.Rental when booking.State != TripState.Cancelled => booking.Total - TripSummary.Paid(booking),
            PaymentKind.DepositReceived when booking.State is TripState.Confirmed or TripState.Active =>
                booking.Deposit - TripSummary.DepositReceived(booking),
            PaymentKind.DepositRefunded when booking.State is TripState.Finished or TripState.Cancelled =>
                TripSummary.DepositBalance(booking),
            PaymentKind.DepositWithheld when booking.State == TripState.Finished => TripSummary.DepositBalance(booking),
            _ => 0
        };
        if (amount > allowed)
            throw new RentalError($"Для этой операции доступно {Math.Max(0, allowed)} BYN. Проверьте статус поездки.");

        db.Payments.Add(new Payment
        {
            BookingId = id, StaffId = staffId, Kind = kind, Amount = amount, RequestId = requestId,
            Note = note?.Trim() ?? "", CreatedAt = Now
        });
        await Notify(booking, $"{TripSummary.PaymentName(kind)}: {amount} BYN.");
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}

