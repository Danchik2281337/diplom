namespace FormaDrive.Data;

public sealed class Inspection
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public int StaffId { get; set; }
    public bool IsReturn { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Mileage { get; set; }
    public required string Notes { get; set; }
    public required string DamageZones { get; set; }
    public List<InspectionPhoto> Photos { get; set; } = [];
}

public sealed class InspectionPhoto
{
    public int Id { get; set; }
    public int InspectionId { get; set; }
    public Inspection Inspection { get; set; } = null!;
    public required byte[] Content { get; set; }
    public required string ContentType { get; set; }
}

public enum PaymentKind { Rental, DepositReceived, DepositRefunded, DepositWithheld }

public sealed class Payment
{
    public int Id { get; set; }
    public Guid RequestId { get; set; }
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public int StaffId { get; set; }
    public PaymentKind Kind { get; set; }
    public int Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public required string Note { get; set; }
}

public sealed class Notification
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public int BookingId { get; set; }
    public required string Text { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

