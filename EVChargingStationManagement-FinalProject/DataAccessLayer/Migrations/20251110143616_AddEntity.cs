using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_review",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargingStationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_review", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_review_tbl_charging_station_ChargingStationId",
                        column: x => x.ChargingStationId,
                        principalTable: "tbl_charging_station",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_review_tbl_user_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LicensePlate = table.Column<string>(type: "text", nullable: false),
                    Make = table.Column<string>(type: "text", nullable: true),
                    Model = table.Column<string>(type: "text", nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: true),
                    Vin = table.Column<string>(type: "text", nullable: true),
                    ConnectorType = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehicles_tbl_user_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargingStationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargingSpotId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_booking", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_booking_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_booking_tbl_charging_spot_ChargingSpotId",
                        column: x => x.ChargingSpotId,
                        principalTable: "tbl_charging_spot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_booking_tbl_charging_station_ChargingStationId",
                        column: x => x.ChargingStationId,
                        principalTable: "tbl_charging_station",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_booking_tbl_user_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tbl_booking_payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    VnpTxnRef = table.Column<string>(type: "text", nullable: true),
                    VnpTransactionNo = table.Column<string>(type: "text", nullable: true),
                    BankCode = table.Column<string>(type: "text", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_booking_payment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_booking_payment_tbl_booking_BookingId",
                        column: x => x.BookingId,
                        principalTable: "tbl_booking",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_charging_session",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargingStationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargingSpotId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EnergyKwh = table.Column<decimal>(type: "numeric", nullable: true),
                    PricePerKwh = table.Column<decimal>(type: "numeric", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_charging_session", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_charging_session_tbl_booking_BookingId",
                        column: x => x.BookingId,
                        principalTable: "tbl_booking",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_charging_session_tbl_charging_spot_ChargingSpotId",
                        column: x => x.ChargingSpotId,
                        principalTable: "tbl_charging_spot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_charging_session_tbl_charging_station_ChargingStationId",
                        column: x => x.ChargingStationId,
                        principalTable: "tbl_charging_station",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargingSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentMethod = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReferenceCode = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_tbl_charging_session_ChargingSessionId",
                        column: x => x.ChargingSessionId,
                        principalTable: "tbl_charging_session",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_booking_ChargingSpotId",
                table: "tbl_booking",
                column: "ChargingSpotId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_booking_ChargingStationId",
                table: "tbl_booking",
                column: "ChargingStationId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_booking_EndTime",
                table: "tbl_booking",
                column: "EndTime");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_booking_StartTime",
                table: "tbl_booking",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_booking_UserId",
                table: "tbl_booking",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_booking_VehicleId",
                table: "tbl_booking",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_booking_payment_BookingId",
                table: "tbl_booking_payment",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_booking_payment_Status",
                table: "tbl_booking_payment",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_booking_payment_VnpTxnRef",
                table: "tbl_booking_payment",
                column: "VnpTxnRef");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_charging_session_BookingId",
                table: "tbl_charging_session",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_charging_session_ChargingSpotId",
                table: "tbl_charging_session",
                column: "ChargingSpotId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_charging_session_ChargingStationId",
                table: "tbl_charging_session",
                column: "ChargingStationId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_review_ChargingStationId",
                table: "tbl_review",
                column: "ChargingStationId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_review_UserId",
                table: "tbl_review",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ChargingSessionId",
                table: "Transactions",
                column: "ChargingSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_LicensePlate",
                table: "Vehicles",
                column: "LicensePlate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_UserId",
                table: "Vehicles",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_booking_payment");

            migrationBuilder.DropTable(
                name: "tbl_review");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "tbl_charging_session");

            migrationBuilder.DropTable(
                name: "tbl_booking");

            migrationBuilder.DropTable(
                name: "Vehicles");
        }
    }
}
