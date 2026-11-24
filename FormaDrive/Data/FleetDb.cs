using Microsoft.EntityFrameworkCore;

namespace FormaDrive.Data;

public sealed class Car
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Photo { get; set; }
    public required string Body { get; set; }
    public required string Transmission { get; set; }
    public required string Color { get; set; }
    public int Seats { get; set; }
    public bool Premium { get; set; }
    public int Rate { get; set; }
    public int Deposit { get; set; }
    public int Mileage { get; set; }
}

public sealed class Member
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public string PasswordHash { get; set; } = "";
    public bool Staff { get; set; }
}

public enum TripState { Confirmed, Active, Finished, Cancelled }

public sealed class Booking
{
    public int Id { get; set; }
    public int CarId { get; set; }
    public Car Car { get; set; } = null!;
    public int MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public TripState State { get; set; }
    public required string Location { get; set; }
    public bool Seat { get; set; }
    public int DailyRate { get; set; }
    public int Deposit { get; set; }
    public int Extras { get; set; }
    public int Total { get; set; }
    public int? OutKm { get; set; }
    public int? InKm { get; set; }
    public string? OutNotes { get; set; }
    public string? InNotes { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public List<Inspection> Inspections { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
}

public sealed class Favorite
{
    public int MemberId { get; set; }
    public int CarId { get; set; }
    public Car Car { get; set; } = null!;
}

public sealed class ServicePeriod
{
    public int Id { get; set; }
    public int CarId { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}

public sealed class FleetDb(DbContextOptions<FleetDb> options) : DbContext(options)
{
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<ServicePeriod> ServicePeriods => Set<ServicePeriod>();
    public DbSet<Inspection> Inspections => Set<Inspection>();
    public DbSet<InspectionPhoto> InspectionPhotos => Set<InspectionPhoto>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.HasDefaultSchema("dbo");
        m.Entity<Car>().HasData(DemoFleet.Cars);
        m.Entity<Car>().Property(x => x.Name).HasMaxLength(100);
        m.Entity<Car>().Property(x => x.Photo).HasMaxLength(80);
        m.Entity<Car>().Property(x => x.Body).HasMaxLength(60);
        m.Entity<Car>().Property(x => x.Color).HasMaxLength(60);
        m.Entity<Car>().Property(x => x.Transmission).HasMaxLength(60);
        m.Entity<Member>().Property(x => x.Name).HasMaxLength(80);
        m.Entity<Member>().Property(x => x.PasswordHash).HasMaxLength(512);
        m.Entity<Booking>().Property(x => x.Location).HasMaxLength(30);
        m.Entity<Booking>().Property(x => x.OutNotes).HasMaxLength(500);
        m.Entity<Booking>().Property(x => x.InNotes).HasMaxLength(500);
        m.Entity<Member>().Property(x => x.Email).HasMaxLength(254);
        m.Entity<Member>().HasIndex(x => x.Email).IsUnique().HasDatabaseName("Email");
        m.Entity<Favorite>().HasKey(x => new { x.MemberId, x.CarId });
        m.Entity<Favorite>().HasIndex(x => x.CarId).HasDatabaseName("CarId");
        m.Entity<Booking>().HasIndex(x => x.MemberId).HasDatabaseName("MemberId");
        m.Entity<Favorite>().HasOne<Member>().WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Favorite>().HasOne(x => x.Car).WithMany().HasForeignKey(x => x.CarId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<ServicePeriod>().HasOne<Car>().WithMany().HasForeignKey(x => x.CarId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Booking>().HasOne(x => x.Car).WithMany().HasForeignKey(x => x.CarId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Booking>().HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Booking>().HasIndex(x => new { x.CarId, x.Start, x.End }).HasDatabaseName("IX_Bookings_Car_Start_End");
        m.Entity<ServicePeriod>().HasIndex(x => new { x.CarId, x.Start, x.End }).HasDatabaseName("IX_Service_Car_Start_End");
        m.Entity<Inspection>().Property(x => x.Notes).HasMaxLength(500);
        m.Entity<Inspection>().Property(x => x.DamageZones).HasMaxLength(150);
        m.Entity<Inspection>().HasIndex(x => new { x.BookingId, x.IsReturn }).IsUnique();
        m.Entity<Inspection>().HasOne(x => x.Booking).WithMany(x => x.Inspections).HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Inspection>().HasOne<Member>().WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<InspectionPhoto>().Property(x => x.ContentType).HasMaxLength(20);
        m.Entity<InspectionPhoto>().HasOne(x => x.Inspection).WithMany(x => x.Photos).HasForeignKey(x => x.InspectionId).OnDelete(DeleteBehavior.Cascade);
        m.Entity<Payment>().Property(x => x.Note).HasMaxLength(500);
        m.Entity<Payment>().HasIndex(x => x.RequestId).IsUnique();
        m.Entity<Payment>().HasOne(x => x.Booking).WithMany(x => x.Payments).HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Payment>().HasOne<Member>().WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Notification>().Property(x => x.Text).HasMaxLength(500);
        m.Entity<Notification>().HasIndex(x => new { x.MemberId, x.ReadAt });
        m.Entity<Notification>().HasOne<Member>().WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Notification>().HasOne<Booking>().WithMany().HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Restrict);
    }
}
