using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddChargingSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_charging_session",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargingStationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargingSpotId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EnergyConsumed = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaymentMethod = table.Column<string>(type: "text", nullable: true),
                    TransactionId = table.Column<string>(type: "text", nullable: true),
                    CurrentSoC = table.Column<int>(type: "integer", nullable: true),
                    TargetSoC = table.Column<int>(type: "integer", nullable: true),
                    PowerOutput = table.Column<decimal>(type: "numeric", nullable: true),
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
                        name: "FK_tbl_charging_session_tbl_charging_station_ChargingStationId",
                        column: x => x.ChargingStationId,
                        principalTable: "tbl_charging_station",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_charging_session_tbl_user_UserId",
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
                name: "IX_tbl_charging_session_ChargingStationId",
                table: "tbl_charging_session",
                column: "ChargingStationId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_charging_session_StartTime",
                table: "tbl_charging_session",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_charging_session_Status",
                table: "tbl_charging_session",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_charging_session_UserId",
                table: "tbl_charging_session",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_charging_session");
        }
    }
}
