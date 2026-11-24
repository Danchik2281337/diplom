using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FormaDrive.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Cars",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Photo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Transmission = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Seats = table.Column<int>(type: "int", nullable: false),
                    Premium = table.Column<bool>(type: "bit", nullable: false),
                    Rate = table.Column<int>(type: "int", nullable: false),
                    Deposit = table.Column<int>(type: "int", nullable: false),
                    Mileage = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Members",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Staff = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServicePeriods",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarId = table.Column<int>(type: "int", nullable: false),
                    Start = table.Column<DateTime>(type: "datetime2", nullable: false),
                    End = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicePeriods_Cars_CarId",
                        column: x => x.CarId,
                        principalSchema: "dbo",
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarId = table.Column<int>(type: "int", nullable: false),
                    MemberId = table.Column<int>(type: "int", nullable: false),
                    Start = table.Column<DateTime>(type: "datetime2", nullable: false),
                    End = table.Column<DateTime>(type: "datetime2", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Seat = table.Column<bool>(type: "bit", nullable: false),
                    DailyRate = table.Column<int>(type: "int", nullable: false),
                    Deposit = table.Column<int>(type: "int", nullable: false),
                    Extras = table.Column<int>(type: "int", nullable: false),
                    Total = table.Column<int>(type: "int", nullable: false),
                    OutKm = table.Column<int>(type: "int", nullable: true),
                    InKm = table.Column<int>(type: "int", nullable: true),
                    OutNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookings_Cars_CarId",
                        column: x => x.CarId,
                        principalSchema: "dbo",
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Members_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "dbo",
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Favorites",
                schema: "dbo",
                columns: table => new
                {
                    MemberId = table.Column<int>(type: "int", nullable: false),
                    CarId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favorites", x => new { x.MemberId, x.CarId });
                    table.ForeignKey(
                        name: "FK_Favorites_Cars_CarId",
                        column: x => x.CarId,
                        principalSchema: "dbo",
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Favorites_Members_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "dbo",
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "Cars",
                columns: new[] { "Id", "Body", "Color", "Deposit", "Mileage", "Name", "Photo", "Premium", "Rate", "Seats", "Transmission" },
                values: new object[,]
                {
                    { 1, "Хэтчбек", "Белый", 200, 18400, "Volkswagen Golf", "golf", false, 95, 5, "Автомат" },
                    { 2, "Лифтбек", "Синий", 200, 18400, "Škoda Octavia", "octavia", false, 115, 5, "Автомат" },
                    { 3, "Хэтчбек", "Красный", 200, 18400, "Renault Clio", "clio", false, 75, 5, "Механика" },
                    { 4, "Седан", "Серебристый", 200, 18400, "Toyota Corolla", "corolla", false, 120, 5, "Автомат" },
                    { 5, "Кроссовер", "Оливковый", 200, 18400, "Hyundai Tucson", "tucson", false, 165, 5, "Автомат" },
                    { 6, "Кроссовер", "Тёмно-синий", 200, 18400, "Kia Sportage", "sportage", false, 160, 5, "Автомат" },
                    { 7, "Купе", "Серебристый", 600, 18400, "Porsche 911 Carrera", "carrera", true, 490, 4, "Автомат" },
                    { 8, "Седан", "Графитовый", 600, 18400, "BMW 530i", "530i", true, 285, 5, "Автомат" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Car_Start_End",
                schema: "dbo",
                table: "Bookings",
                columns: new[] { "CarId", "Start", "End" });

            migrationBuilder.CreateIndex(
                name: "MemberId",
                schema: "dbo",
                table: "Bookings",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "CarId",
                schema: "dbo",
                table: "Favorites",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "Email",
                schema: "dbo",
                table: "Members",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Service_Car_Start_End",
                schema: "dbo",
                table: "ServicePeriods",
                columns: new[] { "CarId", "Start", "End" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bookings",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Favorites",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ServicePeriods",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Members",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Cars",
                schema: "dbo");
        }
    }
}
