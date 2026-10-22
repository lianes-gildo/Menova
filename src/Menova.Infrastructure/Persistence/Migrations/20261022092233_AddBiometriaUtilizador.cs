using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Menova.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBiometriaUtilizador : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "biometria_credencial_id",
                table: "utilizadores",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "biometria_dispositivo",
                table: "utilizadores",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "biometria_public_key",
                table: "utilizadores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "biometria_registada_em",
                table: "utilizadores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_utilizadores_biometria_credencial_id",
                table: "utilizadores",
                column: "biometria_credencial_id",
                unique: true,
                filter: "biometria_credencial_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_utilizadores_biometria_credencial_id",
                table: "utilizadores");

            migrationBuilder.DropColumn(
                name: "biometria_credencial_id",
                table: "utilizadores");

            migrationBuilder.DropColumn(
                name: "biometria_dispositivo",
                table: "utilizadores");

            migrationBuilder.DropColumn(
                name: "biometria_public_key",
                table: "utilizadores");

            migrationBuilder.DropColumn(
                name: "biometria_registada_em",
                table: "utilizadores");
        }
    }
}
