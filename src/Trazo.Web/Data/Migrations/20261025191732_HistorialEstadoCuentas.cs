using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Trazo.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class HistorialEstadoCuentas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CambiosEstadoCuenta",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UsuarioId = table.Column<string>(type: "varchar(191)", nullable: false),
                    AdministradorId = table.Column<string>(type: "varchar(191)", nullable: false),
                    EstadoAnterior = table.Column<int>(type: "int", nullable: false),
                    EstadoNuevo = table.Column<int>(type: "int", nullable: false),
                    Motivo = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    CreadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CambiosEstadoCuenta", x => x.Id);
                    table.CheckConstraint("CK_CambioEstadoCuenta_Transicion", "`EstadoAnterior` IN (0, 1) AND `EstadoNuevo` IN (0, 1) AND `EstadoAnterior` <> `EstadoNuevo` AND `UsuarioId` <> `AdministradorId`");
                    table.ForeignKey(
                        name: "FK_CambiosEstadoCuenta_Usuarios_AdministradorId",
                        column: x => x.AdministradorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CambiosEstadoCuenta_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CambiosEstadoCuenta_AdministradorId",
                table: "CambiosEstadoCuenta",
                column: "AdministradorId");

            migrationBuilder.CreateIndex(
                name: "IX_CambiosEstadoCuenta_UsuarioId_CreadoUtc_Id",
                table: "CambiosEstadoCuenta",
                columns: new[] { "UsuarioId", "CreadoUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CambiosEstadoCuenta");
        }
    }
}
