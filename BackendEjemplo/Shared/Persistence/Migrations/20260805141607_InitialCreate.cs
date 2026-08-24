using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BackendEjemplo.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bot_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    server = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    subflujo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    usuario_bot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    plataforma = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    usuario_plataforma = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipo_documento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    nro_documento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    mensaje = table.Column<string>(type: "text", nullable: false),
                    falla = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_bot_logs", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bot_logs");
        }
    }
}
