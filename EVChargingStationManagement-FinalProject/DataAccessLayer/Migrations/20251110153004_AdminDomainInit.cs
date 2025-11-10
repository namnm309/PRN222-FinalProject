using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AdminDomainInit : Migration
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
                    ChargingSpotId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EnergyKwh = table.Column<decimal>(type: "numeric", nullable: true),
                    PricePerKwh = table.Column<decimal>(type: "numeric", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
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
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_charging_session_tbl_charging_station_ChargingStationId",
                        column: x => x.ChargingStationId,
                        principalTable: "tbl_charging_station",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_charging_session_tbl_user_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_customer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ContactEmail = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    TaxId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_customer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_station_staff",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargingStationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_station_staff", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_station_staff_tbl_charging_station_ChargingStationId",
                        column: x => x.ChargingStationId,
                        principalTable: "tbl_charging_station",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_station_staff_tbl_user_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_subscription_plan",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    BillingType = table.Column<int>(type: "integer", nullable: false),
                    PricePerMonth = table.Column<decimal>(type: "numeric", nullable: true),
                    PricePerKwh = table.Column<decimal>(type: "numeric", nullable: true),
                    IncludedKwh = table.Column<decimal>(type: "numeric", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_subscription_plan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_user_subscription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AutoRenew = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_user_subscription", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_user_subscription_tbl_subscription_plan_SubscriptionPla~",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "tbl_subscription_plan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_user_subscription_tbl_user_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateIndex(
                name: "IX_tbl_customer_Type",
                table: "tbl_customer",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_station_staff_ChargingStationId_UserId",
                table: "tbl_station_staff",
                columns: new[] { "ChargingStationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_station_staff_UserId",
                table: "tbl_station_staff",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_subscription_plan_BillingType",
                table: "tbl_subscription_plan",
                column: "BillingType");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_subscription_plan_IsActive",
                table: "tbl_subscription_plan",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_user_subscription_Status",
                table: "tbl_user_subscription",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_user_subscription_SubscriptionPlanId",
                table: "tbl_user_subscription",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_user_subscription_UserId",
                table: "tbl_user_subscription",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_charging_session");

            migrationBuilder.DropTable(
                name: "tbl_customer");

            migrationBuilder.DropTable(
                name: "tbl_station_staff");

            migrationBuilder.DropTable(
                name: "tbl_user_subscription");

            migrationBuilder.DropTable(
                name: "tbl_subscription_plan");
        }
    }
}
