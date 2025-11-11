using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class updateentity2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EWalletProvider",
                table: "tbl_payment_transaction",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionPackageId",
                table: "tbl_payment_transaction",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WalletBalanceAfter",
                table: "tbl_payment_transaction",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WalletBalanceBefore",
                table: "tbl_payment_transaction",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExternalRating",
                table: "tbl_charging_station",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExternalReviewCount",
                table: "tbl_charging_station",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFromSerpApi",
                table: "tbl_charging_station",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SerpApiLastSynced",
                table: "tbl_charging_station",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerpApiPlaceId",
                table: "tbl_charging_station",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "tbl_charging_spot",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "QrCode",
                table: "tbl_charging_spot",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentPowerKw",
                table: "tbl_charging_session",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentSocPercentage",
                table: "tbl_charging_session",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InitialSocPercentage",
                table: "tbl_charging_session",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "tbl_charging_session",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrCodeScanned",
                table: "tbl_charging_session",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetSocPercentage",
                table: "tbl_charging_session",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tbl_charging_session_progress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargingSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SocPercentage = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PowerKw = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    EnergyDeliveredKwh = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EstimatedTimeRemainingMinutes = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_charging_session_progress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_charging_session_progress_tbl_charging_session_Charging~",
                        column: x => x.ChargingSessionId,
                        principalTable: "tbl_charging_session",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_station_report",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargingStationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalSessions = table.Column<int>(type: "integer", nullable: false),
                    TotalEnergyDeliveredKwh = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalRevenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PeakHour = table.Column<int>(type: "integer", nullable: true),
                    AverageSessionDurationMinutes = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_station_report", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_station_report_tbl_charging_station_ChargingStationId",
                        column: x => x.ChargingStationId,
                        principalTable: "tbl_charging_station",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_subscription_package",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DurationDays = table.Column<int>(type: "integer", nullable: false),
                    EnergyKwh = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_subscription_package", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_user_subscription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionPackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RemainingEnergyKwh = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_user_subscription", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_user_subscription_tbl_subscription_package_Subscription~",
                        column: x => x.SubscriptionPackageId,
                        principalTable: "tbl_subscription_package",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_user_subscription_tbl_user_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_payment_transaction_SubscriptionPackageId",
                table: "tbl_payment_transaction",
                column: "SubscriptionPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_charging_station_IsFromSerpApi",
                table: "tbl_charging_station",
                column: "IsFromSerpApi");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_charging_station_SerpApiPlaceId",
                table: "tbl_charging_station",
                column: "SerpApiPlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_charging_spot_IsOnline",
                table: "tbl_charging_spot",
                column: "IsOnline");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_charging_spot_QrCode",
                table: "tbl_charging_spot",
                column: "QrCode");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_charging_session_progress_ChargingSessionId",
                table: "tbl_charging_session_progress",
                column: "ChargingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_charging_session_progress_ChargingSessionId_RecordedAt",
                table: "tbl_charging_session_progress",
                columns: new[] { "ChargingSessionId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_charging_session_progress_RecordedAt",
                table: "tbl_charging_session_progress",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_station_report_ChargingStationId",
                table: "tbl_station_report",
                column: "ChargingStationId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_station_report_ChargingStationId_ReportDate",
                table: "tbl_station_report",
                columns: new[] { "ChargingStationId", "ReportDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_station_report_ReportDate",
                table: "tbl_station_report",
                column: "ReportDate");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_subscription_package_IsActive",
                table: "tbl_subscription_package",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_subscription_package_Name",
                table: "tbl_subscription_package",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_user_subscription_IsActive",
                table: "tbl_user_subscription",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_user_subscription_SubscriptionPackageId",
                table: "tbl_user_subscription",
                column: "SubscriptionPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_user_subscription_UserId",
                table: "tbl_user_subscription",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_user_subscription_UserId_SubscriptionPackageId",
                table: "tbl_user_subscription",
                columns: new[] { "UserId", "SubscriptionPackageId" });

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_payment_transaction_tbl_subscription_package_Subscripti~",
                table: "tbl_payment_transaction",
                column: "SubscriptionPackageId",
                principalTable: "tbl_subscription_package",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl_payment_transaction_tbl_subscription_package_Subscripti~",
                table: "tbl_payment_transaction");

            migrationBuilder.DropTable(
                name: "tbl_charging_session_progress");

            migrationBuilder.DropTable(
                name: "tbl_station_report");

            migrationBuilder.DropTable(
                name: "tbl_user_subscription");

            migrationBuilder.DropTable(
                name: "tbl_subscription_package");

            migrationBuilder.DropIndex(
                name: "IX_tbl_payment_transaction_SubscriptionPackageId",
                table: "tbl_payment_transaction");

            migrationBuilder.DropIndex(
                name: "IX_tbl_charging_station_IsFromSerpApi",
                table: "tbl_charging_station");

            migrationBuilder.DropIndex(
                name: "IX_tbl_charging_station_SerpApiPlaceId",
                table: "tbl_charging_station");

            migrationBuilder.DropIndex(
                name: "IX_tbl_charging_spot_IsOnline",
                table: "tbl_charging_spot");

            migrationBuilder.DropIndex(
                name: "IX_tbl_charging_spot_QrCode",
                table: "tbl_charging_spot");

            migrationBuilder.DropColumn(
                name: "EWalletProvider",
                table: "tbl_payment_transaction");

            migrationBuilder.DropColumn(
                name: "SubscriptionPackageId",
                table: "tbl_payment_transaction");

            migrationBuilder.DropColumn(
                name: "WalletBalanceAfter",
                table: "tbl_payment_transaction");

            migrationBuilder.DropColumn(
                name: "WalletBalanceBefore",
                table: "tbl_payment_transaction");

            migrationBuilder.DropColumn(
                name: "ExternalRating",
                table: "tbl_charging_station");

            migrationBuilder.DropColumn(
                name: "ExternalReviewCount",
                table: "tbl_charging_station");

            migrationBuilder.DropColumn(
                name: "IsFromSerpApi",
                table: "tbl_charging_station");

            migrationBuilder.DropColumn(
                name: "SerpApiLastSynced",
                table: "tbl_charging_station");

            migrationBuilder.DropColumn(
                name: "SerpApiPlaceId",
                table: "tbl_charging_station");

            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "tbl_charging_spot");

            migrationBuilder.DropColumn(
                name: "QrCode",
                table: "tbl_charging_spot");

            migrationBuilder.DropColumn(
                name: "CurrentPowerKw",
                table: "tbl_charging_session");

            migrationBuilder.DropColumn(
                name: "CurrentSocPercentage",
                table: "tbl_charging_session");

            migrationBuilder.DropColumn(
                name: "InitialSocPercentage",
                table: "tbl_charging_session");

            migrationBuilder.DropColumn(
                name: "LastUpdatedAt",
                table: "tbl_charging_session");

            migrationBuilder.DropColumn(
                name: "QrCodeScanned",
                table: "tbl_charging_session");

            migrationBuilder.DropColumn(
                name: "TargetSocPercentage",
                table: "tbl_charging_session");
        }
    }
}
