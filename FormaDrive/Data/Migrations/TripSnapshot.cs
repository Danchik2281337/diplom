using Microsoft.EntityFrameworkCore;

namespace FormaDrive.Data.Migrations;

// Snapshot definitions are kept separate from the live entities for future migrations.
partial class FleetDbModelSnapshot
{
    private static void AddTripFeatures(ModelBuilder model)
    {
        model.Entity("FormaDrive.Data.Booking", b =>
        {
            b.Property<DateTime?>("IssuedAt").HasColumnType("datetime2");
            b.Property<DateTime?>("ReturnedAt").HasColumnType("datetime2");
        });
        model.Entity("FormaDrive.Data.Inspection", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int").UseIdentityColumn();
            b.Property<int>("BookingId").HasColumnType("int");
            b.Property<int>("StaffId").HasColumnType("int");
            b.Property<bool>("IsReturn").HasColumnType("bit");
            b.Property<DateTime>("CreatedAt").HasColumnType("datetime2");
            b.Property<int>("Mileage").HasColumnType("int");
            b.Property<string>("Notes").IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<string>("DamageZones").IsRequired().HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.HasKey("Id");
            b.HasIndex("BookingId", "IsReturn").IsUnique();
            b.HasIndex("StaffId");
            b.ToTable("Inspections", "dbo");
            b.HasOne("FormaDrive.Data.Booking", "Booking").WithMany("Inspections").HasForeignKey("BookingId").OnDelete(DeleteBehavior.Restrict).IsRequired();
            b.HasOne("FormaDrive.Data.Member", null).WithMany().HasForeignKey("StaffId").OnDelete(DeleteBehavior.Restrict).IsRequired();
            b.Navigation("Booking");
        });
        model.Entity("FormaDrive.Data.InspectionPhoto", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int").UseIdentityColumn();
            b.Property<int>("InspectionId").HasColumnType("int");
            b.Property<byte[]>("Content").IsRequired().HasColumnType("varbinary(max)");
            b.Property<string>("ContentType").IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            b.HasKey("Id");
            b.HasIndex("InspectionId");
            b.ToTable("InspectionPhotos", "dbo");
            b.HasOne("FormaDrive.Data.Inspection", "Inspection").WithMany("Photos").HasForeignKey("InspectionId").OnDelete(DeleteBehavior.Cascade).IsRequired();
            b.Navigation("Inspection");
        });
        model.Entity("FormaDrive.Data.Payment", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int").UseIdentityColumn();
            b.Property<int>("BookingId").HasColumnType("int");
            b.Property<Guid>("RequestId").HasColumnType("uniqueidentifier");
            b.Property<int>("StaffId").HasColumnType("int");
            b.Property<int>("Kind").HasColumnType("int");
            b.Property<int>("Amount").HasColumnType("int");
            b.Property<DateTime>("CreatedAt").HasColumnType("datetime2");
            b.Property<string>("Note").IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.HasKey("Id");
            b.HasIndex("BookingId");
            b.HasIndex("RequestId").IsUnique();
            b.HasIndex("StaffId");
            b.ToTable("Payments", "dbo");
            b.HasOne("FormaDrive.Data.Booking", "Booking").WithMany("Payments").HasForeignKey("BookingId").OnDelete(DeleteBehavior.Restrict).IsRequired();
            b.HasOne("FormaDrive.Data.Member", null).WithMany().HasForeignKey("StaffId").OnDelete(DeleteBehavior.Restrict).IsRequired();
            b.Navigation("Booking");
        });
        model.Entity("FormaDrive.Data.Notification", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int").UseIdentityColumn();
            b.Property<int>("MemberId").HasColumnType("int");
            b.Property<int>("BookingId").HasColumnType("int");
            b.Property<string>("Text").IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<DateTime>("CreatedAt").HasColumnType("datetime2");
            b.Property<DateTime?>("ReadAt").HasColumnType("datetime2");
            b.HasKey("Id");
            b.HasIndex("BookingId");
            b.HasIndex("MemberId", "ReadAt");
            b.ToTable("Notifications", "dbo");
            b.HasOne("FormaDrive.Data.Member", null).WithMany().HasForeignKey("MemberId").OnDelete(DeleteBehavior.Restrict).IsRequired();
            b.HasOne("FormaDrive.Data.Booking", null).WithMany().HasForeignKey("BookingId").OnDelete(DeleteBehavior.Restrict).IsRequired();
        });
    }
}
