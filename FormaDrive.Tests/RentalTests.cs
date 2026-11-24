using FormaDrive.Services;
using FormaDrive.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class RentalTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(1440, 1)]
    [InlineData(1441, 2)]
    [InlineData(2880, 2)]
    public void StartedDays(int minutes, int billed)
    {
        var start = new DateTime(2030, 6, 1, 18, 0, 0);
        Assert.Equal(billed, Tariff.Days(start, start.AddMinutes(minutes)));
    }

    [Fact]
    public void ExtrasArePerTrip() => Assert.Equal(50, Tariff.Extras(Tariff.Airport, true));

    [Fact]
    public void EndMustFollowStart()
    {
        var start = DateTime.Today.AddDays(1);
        Assert.Throws<RentalError>(() => Tariff.Validate(start, start, Tariff.Office, start.AddDays(-1)));
    }

    [Fact]
    [Trait("Category", "SqlServer")]
    public async Task OneWinnerAndFullRentalLifecycle()
    {
        var connection = Environment.GetEnvironmentVariable("FORMA_TEST_SQLSERVER")
            ?? throw new InvalidOperationException("Set FORMA_TEST_SQLSERVER to an isolated SQL Server test database.");
        if (!new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection).InitialCatalog.EndsWith("_test", StringComparison.Ordinal))
            throw new InvalidOperationException("Use a database ending in _test.");
        var options = new DbContextOptionsBuilder<FleetDb>().UseSqlServer(connection).Options;
        await using var db = new FleetDb(options);
        await db.Database.MigrateAsync();
        var member = new Member { Name = "Test", Email = Guid.NewGuid() + "@example.test" };
        var car = new Car { Name = "Test", Photo = "golf", Body = "Hatch", Transmission = "Automatic", Color = "White", Rate = 100, Seats = 5, Deposit = 200 };
        db.AddRange(member, car);
        await db.SaveChangesAsync();
        var clock = new TripClock();
        var service = new Rentals(db, clock);
        var start = service.Now.AddHours(1);
        var end = start.AddDays(2);
        async Task<int?> Attempt()
        {
            await using var client = new FleetDb(options);
            try { return await new Rentals(client, clock).Book(member.Id, car.Id, start, end, Tariff.Airport, true, 250); }
            catch (RentalError) { return null; }
        }
        var attempts = await Task.WhenAll(Attempt(), Attempt());
        var id = Assert.Single(attempts, x => x.HasValue)!.Value;
        var next = await service.Book(member.Id, car.Id, end, end.AddDays(1), Tariff.Office, false, 100);
        await Assert.ThrowsAsync<RentalError>(() => service.Change(id, member.Id, "extend", end.AddHours(1)));
        await service.Change(next, member.Id, "cancel");
        await service.Change(id, member.Id, "extend", end.AddDays(1));
        clock.Instant = clock.Instant.AddHours(1);
        await service.Change(id, null, "issue", km: 100, notes: "OK");
        await Assert.ThrowsAsync<RentalError>(() => service.Change(id, null, "return", km: 99));
        await service.Change(id, null, "return", km: 150, notes: "OK");
        var booking = await db.Bookings.AsNoTracking().SingleAsync(b => b.Id == id);
        Assert.Equal(350, booking.Total);
        Assert.Equal(TripState.Finished, booking.State);
        await service.Maintenance(car.Id, end, end.AddDays(1));
        Assert.False(await service.Available(end, end.AddHours(1)).AnyAsync(c => c.Id == car.Id));
    }

    private sealed class TripClock : TimeProvider
    {
        public DateTimeOffset Instant { get; set; } = new(2030, 6, 1, 8, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Instant;
    }
}
