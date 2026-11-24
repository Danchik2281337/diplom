using FormaDrive.Data;

namespace FormaDrive.Services;

public static class TripSummary
{
    public static readonly Dictionary<string, string> DamageZones = new()
    {
        ["front"] = "Спереди", ["rear"] = "Сзади", ["left"] = "Левый борт",
        ["right"] = "Правый борт", ["roof"] = "Крыша", ["interior"] = "Салон"
    };

    public static int Paid(Booking b) => Sum(b, PaymentKind.Rental);
    public static int DepositReceived(Booking b) => Sum(b, PaymentKind.DepositReceived);
    public static int DepositRefunded(Booking b) => Sum(b, PaymentKind.DepositRefunded);
    public static int DepositWithheld(Booking b) => Sum(b, PaymentKind.DepositWithheld);
    public static int DepositBalance(Booking b) => DepositReceived(b) - DepositRefunded(b) - DepositWithheld(b);
    private static int Sum(Booking b, PaymentKind kind) => b.Payments.Where(p => p.Kind == kind).Sum(p => p.Amount);

    public static string PaymentName(PaymentKind kind) => kind switch
    {
        PaymentKind.Rental => "Оплата аренды",
        PaymentKind.DepositReceived => "Залог получен",
        PaymentKind.DepositRefunded => "Залог возвращён",
        PaymentKind.DepositWithheld => "Удержание из залога",
        _ => "Операция"
    };

    public static bool Overdue(Booking b, DateTime now) => b.State == TripState.Active && b.End < now;
    public static string Delay(Booking b, DateTime now)
    {
        var minutes = Math.Max(0, (int)(now - b.End).TotalMinutes);
        return $"{minutes / 60} ч {minutes % 60} мин";
    }

    public static string Zones(string value) => string.Join(", ",
        value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(key => DamageZones.GetValueOrDefault(key, key)));
}

