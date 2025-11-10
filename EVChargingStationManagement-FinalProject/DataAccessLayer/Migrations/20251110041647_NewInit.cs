using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class NewInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_charging_station",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "text", nullable: true),
                    Province = table.Column<string>(type: "text", nullable: true),
                    PostalCode = table.Column<string>(type: "text", nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric", nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    OpeningTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    ClosingTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    Is24Hours = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_charging_station", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_user",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Gender = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    GoogleId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_user", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_charging_spot",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpotNumber = table.Column<string>(type: "text", nullable: false),
                    ChargingStationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ConnectorType = table.Column<string>(type: "text", nullable: true),
                    PowerOutput = table.Column<decimal>(type: "numeric", nullable: true),
                    PricePerKwh = table.Column<decimal>(type: "numeric", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_charging_spot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_charging_spot_tbl_charging_station_ChargingStationId",
                        column: x => x.ChargingStationId,
                        principalTable: "tbl_charging_station",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_refresh_token",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_refresh_token", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_refresh_token_tbl_user_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_station_error",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargingStationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargingSpotId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReportedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolvedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorCode = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "text", nullable: true),
                    Severity = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_station_error", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_station_error_tbl_charging_spot_ChargingSpotId",
                        column: x => x.ChargingSpotId,
                        principalTable: "tbl_charging_spot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_station_error_tbl_charging_station_ChargingStationId",
                        column: x => x.ChargingStationId,
                        principalTable: "tbl_charging_station",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_station_error_tbl_user_ReportedByUserId",
                        column: x => x.ReportedByUserId,
                        principalTable: "tbl_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_station_error_tbl_user_ResolvedByUserId",
                        column: x => x.ResolvedByUserId,
                        principalTable: "tbl_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tbl_station_maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargingStationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargingSpotId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReportedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScheduledDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_station_maintenance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_station_maintenance_tbl_charging_spot_ChargingSpotId",
                        column: x => x.ChargingSpotId,
                        principalTable: "tbl_charging_spot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_station_maintenance_tbl_charging_station_ChargingStatio~",
                        column: x => x.ChargingStationId,
                        principalTable: "tbl_charging_station",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_station_maintenance_tbl_user_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "tbl_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_station_maintenance_tbl_user_ReportedByUserId",
                        column: x => x.ReportedByUserId,
                        principalTable: "tbl_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_charging_spot_ChargingStationId",
                table: "tbl_charging_spot",
                column: "ChargingStationId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_charging_spot_SpotNumber",
                table: "tbl_charging_spot",
                column: "SpotNumber");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_charging_spot_Status",
                table: "tbl_charging_spot",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_charging_station_Name",
                table: "tbl_charging_station",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_charging_station_Status",
                table: "tbl_charging_station",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_refresh_token_Token",
                table: "tbl_refresh_token",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_refresh_token_UserId",
                table: "tbl_refresh_token",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_station_error_ChargingSpotId",
                table: "tbl_station_error",
                column: "ChargingSpotId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_station_error_ChargingStationId",
                table: "tbl_station_error",
                column: "ChargingStationId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_station_error_ErrorCode",
                table: "tbl_station_error",
                column: "ErrorCode");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_station_error_ReportedAt",
                table: "tbl_station_error",
                column: "ReportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_station_error_ReportedByUserId",
                table: "tbl_station_error",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_station_error_ResolvedByUserId",
                table: "tbl_station_error",
                column: "ResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_station_error_Status",
                table: "tbl_station_error",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_station_maintenance_AssignedToUserId",
                table: "tbl_station_maintenance",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_station_maintenance_ChargingSpotId",
                table: "tbl_station_maintenance",
                column: "ChargingSpotId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_station_maintenance_ChargingStationId",
                table: "tbl_station_maintenance",
                column: "ChargingStationId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_station_maintenance_ReportedByUserId",
                table: "tbl_station_maintenance",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_station_maintenance_ScheduledDate",
                table: "tbl_station_maintenance",
                column: "ScheduledDate");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_station_maintenance_Status",
                table: "tbl_station_maintenance",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_refresh_token");

            migrationBuilder.DropTable(
                name: "tbl_station_error");

            migrationBuilder.DropTable(
                name: "tbl_station_maintenance");

            migrationBuilder.DropTable(
                name: "tbl_charging_spot");

            migrationBuilder.DropTable(
                name: "tbl_user");

            migrationBuilder.DropTable(
                name: "tbl_charging_station");
        }
    }
}
