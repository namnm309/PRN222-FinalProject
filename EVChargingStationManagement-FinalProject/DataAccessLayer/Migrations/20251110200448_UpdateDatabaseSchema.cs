using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabaseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BookingId",
                table: "tbl_charging_session",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndedAt",
                table: "tbl_charging_session",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EnergyKwh",
                table: "tbl_charging_session",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerKwh",
                table: "tbl_charging_session",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "tbl_charging_session",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "tbl_charging_session",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookingId",
                table: "tbl_charging_session");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "tbl_charging_session");

            migrationBuilder.DropColumn(
                name: "EnergyKwh",
                table: "tbl_charging_session");

            migrationBuilder.DropColumn(
                name: "PricePerKwh",
                table: "tbl_charging_session");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "tbl_charging_session");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "tbl_charging_session");
        }
    }
}
