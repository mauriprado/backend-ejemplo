using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendEjemplo.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RestrictClientOrderDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_orders_clients_client_id",
                table: "orders");

            migrationBuilder.AddForeignKey(
                name: "f_k_orders_clients_client_id",
                table: "orders",
                column: "client_id",
                principalTable: "clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_orders_clients_client_id",
                table: "orders");

            migrationBuilder.AddForeignKey(
                name: "f_k_orders_clients_client_id",
                table: "orders",
                column: "client_id",
                principalTable: "clients",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
