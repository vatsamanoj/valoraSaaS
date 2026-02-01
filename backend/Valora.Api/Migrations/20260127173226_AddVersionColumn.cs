using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "StockMovement",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "SalesOrder",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "ObjectRecord",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "ObjectField",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "MaterialMaster",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "JournalEntry",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "GLAccount",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                schema: "secure",
                table: "EmployeePayroll",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Employee",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "CostCenter",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "StockMovement");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SalesOrder");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ObjectRecord");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ObjectField");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MaterialMaster");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "JournalEntry");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "GLAccount");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "secure",
                table: "EmployeePayroll");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CostCenter");
        }
    }
}
