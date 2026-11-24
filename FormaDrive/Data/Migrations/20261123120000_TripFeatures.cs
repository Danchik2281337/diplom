using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FormaDrive.Data.Migrations;

[DbContext(typeof(FleetDb))]
[Migration("20261123120000_TripFeatures")]
public sealed class TripFeatures : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
ALTER TABLE dbo.Bookings ADD IssuedAt datetime2 NULL, ReturnedAt datetime2 NULL;
CREATE TABLE dbo.Inspections (
    Id int IDENTITY CONSTRAINT PK_Inspections PRIMARY KEY, BookingId int NOT NULL, StaffId int NOT NULL,
    IsReturn bit NOT NULL, CreatedAt datetime2 NOT NULL, Mileage int NOT NULL,
    Notes nvarchar(500) NOT NULL, DamageZones nvarchar(150) NOT NULL,
    CONSTRAINT FK_Inspections_Bookings_BookingId FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(Id),
    CONSTRAINT FK_Inspections_Members_StaffId FOREIGN KEY (StaffId) REFERENCES dbo.Members(Id)
);
CREATE UNIQUE INDEX IX_Inspections_BookingId_IsReturn ON dbo.Inspections(BookingId,IsReturn);
CREATE INDEX IX_Inspections_StaffId ON dbo.Inspections(StaffId);
CREATE TABLE dbo.InspectionPhotos (
    Id int IDENTITY CONSTRAINT PK_InspectionPhotos PRIMARY KEY, InspectionId int NOT NULL,
    Content varbinary(max) NOT NULL, ContentType nvarchar(20) NOT NULL,
    CONSTRAINT FK_InspectionPhotos_Inspections_InspectionId FOREIGN KEY (InspectionId) REFERENCES dbo.Inspections(Id) ON DELETE CASCADE
);
CREATE INDEX IX_InspectionPhotos_InspectionId ON dbo.InspectionPhotos(InspectionId);
CREATE TABLE dbo.Payments (
    Id int IDENTITY CONSTRAINT PK_Payments PRIMARY KEY, BookingId int NOT NULL, StaffId int NOT NULL,
    Kind int NOT NULL, Amount int NOT NULL, CreatedAt datetime2 NOT NULL, Note nvarchar(500) NOT NULL, RequestId uniqueidentifier NOT NULL,
    CONSTRAINT FK_Payments_Bookings_BookingId FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(Id),
    CONSTRAINT FK_Payments_Members_StaffId FOREIGN KEY (StaffId) REFERENCES dbo.Members(Id)
);
CREATE INDEX IX_Payments_BookingId ON dbo.Payments(BookingId);
CREATE UNIQUE INDEX IX_Payments_RequestId ON dbo.Payments(RequestId);
CREATE INDEX IX_Payments_StaffId ON dbo.Payments(StaffId);
CREATE TABLE dbo.Notifications (
    Id int IDENTITY CONSTRAINT PK_Notifications PRIMARY KEY, MemberId int NOT NULL, BookingId int NOT NULL,
    Text nvarchar(500) NOT NULL, CreatedAt datetime2 NOT NULL, ReadAt datetime2 NULL,
    CONSTRAINT FK_Notifications_Members_MemberId FOREIGN KEY (MemberId) REFERENCES dbo.Members(Id),
    CONSTRAINT FK_Notifications_Bookings_BookingId FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(Id)
);
CREATE INDEX IX_Notifications_MemberId_ReadAt ON dbo.Notifications(MemberId,ReadAt);
CREATE INDEX IX_Notifications_BookingId ON dbo.Notifications(BookingId);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("InspectionPhotos", "dbo");
        migrationBuilder.DropTable("Inspections", "dbo");
        migrationBuilder.DropTable("Payments", "dbo");
        migrationBuilder.DropTable("Notifications", "dbo");
        migrationBuilder.DropColumn("IssuedAt", "Bookings", "dbo");
        migrationBuilder.DropColumn("ReturnedAt", "Bookings", "dbo");
    }
}
