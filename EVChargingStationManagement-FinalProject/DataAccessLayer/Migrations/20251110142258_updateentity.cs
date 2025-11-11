using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class updateentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_notification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReferenceId = table.Column<string>(type: "text", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_notification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_notification_tbl_user_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_station_amenity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargingStationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_station_amenity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_station_amenity_tbl_charging_station_ChargingStationId",
                        column: x => x.ChargingStationId,
                        principalTable: "tbl_charging_station",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_vehicle",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Make = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    ModelYear = table.Column<int>(type: "integer", nullable: true),
                    LicensePlate = table.Column<string>(type: "text", nullable: true),
                    Vin = table.Column<string>(type: "text", nullable: true),
                    VehicleType = table.Column<int>(type: "integer", nullable: false),
                    BatteryCapacityKwh = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxChargingPowerKw = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Color = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_vehicle", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_reservation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargingSpotId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScheduledStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScheduledEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ConfirmationCode = table.Column<string>(type: "text", nullable: false),
                    EstimatedEnergyKwh = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsPrepaid = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_reservation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_reservation_tbl_charging_spot_ChargingSpotId",
                        column: x => x.ChargingSpotId,
                        principalTable: "tbl_charging_spot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_reservation_tbl_user_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_reservation_tbl_vehicle_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "tbl_vehicle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tbl_user_vehicle",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    Nickname = table.Column<string>(type: "text", nullable: true),
                    ChargePortLocation = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_user_vehicle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_user_vehicle_tbl_user_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_user_vehicle_tbl_vehicle_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "tbl_vehicle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_charging_session",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargingSpotId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: true),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SessionStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SessionEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EnergyDeliveredKwh = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    EnergyRequestedKwh = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    PricePerKwh = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ExternalSessionId = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_charging_session", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_charging_session_tbl_charging_spot_ChargingSpotId",
                        column: x => x.ChargingSpotId,
                        principalTable: "tbl_charging_spot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_charging_session_tbl_reservation_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "tbl_reservation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tbl_charging_session_tbl_user_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_charging_session_tbl_vehicle_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "tbl_vehicle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tbl_payment_transaction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChargingSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "text", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_payment_transaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_payment_transaction_tbl_charging_session_ChargingSessio~",
                        column: x => x.ChargingSessionId,
                        principalTable: "tbl_charging_session",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tbl_payment_transaction_tbl_reservation_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "tbl_reservation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tbl_payment_transaction_tbl_user_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_charging_session_ChargingSpotId",
                table: "tbl_charging_session",
                column: "ChargingSpotId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_charging_session_ReservationId",
                table: "tbl_charging_session",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_charging_session_Status",
                table: "tbl_charging_session",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_charging_session_UserId",
                table: "tbl_charging_session",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_charging_session_VehicleId",
                table: "tbl_charging_session",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_notification_IsRead",
                table: "tbl_notification",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_notification_Type",
                table: "tbl_notification",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_notification_UserId",
                table: "tbl_notification",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_payment_transaction_ChargingSessionId",
                table: "tbl_payment_transaction",
                column: "ChargingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_payment_transaction_Method",
                table: "tbl_payment_transaction",
                column: "Method");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_payment_transaction_ReservationId",
                table: "tbl_payment_transaction",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_payment_transaction_Status",
                table: "tbl_payment_transaction",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_payment_transaction_UserId",
                table: "tbl_payment_transaction",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_reservation_ChargingSpotId",
                table: "tbl_reservation",
                column: "ChargingSpotId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_reservation_ConfirmationCode",
                table: "tbl_reservation",
                column: "ConfirmationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_reservation_Status",
                table: "tbl_reservation",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_reservation_UserId",
                table: "tbl_reservation",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_reservation_VehicleId",
                table: "tbl_reservation",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_station_amenity_ChargingStationId",
                table: "tbl_station_amenity",
                column: "ChargingStationId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_station_amenity_ChargingStationId_Name",
                table: "tbl_station_amenity",
                columns: new[] { "ChargingStationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_station_amenity_IsActive",
                table: "tbl_station_amenity",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_user_vehicle_UserId_VehicleId",
                table: "tbl_user_vehicle",
                columns: new[] { "UserId", "VehicleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_user_vehicle_VehicleId",
                table: "tbl_user_vehicle",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_vehicle_LicensePlate",
                table: "tbl_vehicle",
                column: "LicensePlate");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_vehicle_Vin",
                table: "tbl_vehicle",
                column: "Vin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_notification");

            migrationBuilder.DropTable(
                name: "tbl_payment_transaction");

            migrationBuilder.DropTable(
                name: "tbl_station_amenity");

            migrationBuilder.DropTable(
                name: "tbl_user_vehicle");

            migrationBuilder.DropTable(
                name: "tbl_charging_session");

            migrationBuilder.DropTable(
                name: "tbl_reservation");

            migrationBuilder.DropTable(
                name: "tbl_vehicle");
        }
    }
}
