using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Menova.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFatoVendasDiarias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fato_vendas_diarias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    artigo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    artigo_nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    categoria_nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    quantidade_vendida = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    total_bruto = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_desconto = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_iva = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_liquido = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fato_vendas_diarias", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fato_vendas_diarias_data",
                table: "fato_vendas_diarias",
                column: "data");

            migrationBuilder.CreateIndex(
                name: "IX_fato_vendas_diarias_data_artigo_id",
                table: "fato_vendas_diarias",
                columns: new[] { "data", "artigo_id" });

            migrationBuilder.CreateIndex(
                name: "IX_fato_vendas_diarias_data_categoria_id",
                table: "fato_vendas_diarias",
                columns: new[] { "data", "categoria_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fato_vendas_diarias");
        }
    }
}
