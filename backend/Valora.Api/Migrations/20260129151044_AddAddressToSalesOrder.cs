using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAddressToSalesOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingAddress",
                table: "SalesOrder",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress",
                table: "SalesOrder",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingAddress",
                table: "SalesOrder");

            migrationBuilder.DropColumn(
                name: "ShippingAddress",
                table: "SalesOrder");
        }
    }
}
