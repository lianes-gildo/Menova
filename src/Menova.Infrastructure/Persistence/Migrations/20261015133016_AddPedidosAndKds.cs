using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Menova.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPedidosAndKds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chaves_idempotencia",
                columns: table => new
                {
                    chave = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    resultado_json = table.Column<string>(type: "jsonb", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expira_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chaves_idempotencia", x => x.chave);
                });

            migrationBuilder.CreateTable(
                name: "contas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    mesa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    total_bruto = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0.00m),
                    total_desconto = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0.00m),
                    total_iva = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0.00m),
                    total_liquido = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0.00m),
                    observacoes = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    fechada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fechada_por = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    criado_por = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    atualizado_por = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    versao = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contas", x => x.id);
                    table.ForeignKey(
                        name: "FK_contas_mesas_mesa_id",
                        column: x => x.mesa_id,
                        principalTable: "mesas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "pedidos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero_sequencial = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    observacoes = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    criado_por = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    atualizado_por = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    versao = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pedidos", x => x.id);
                    table.ForeignKey(
                        name: "FK_pedidos_contas_conta_id",
                        column: x => x.conta_id,
                        principalTable: "contas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "itens_pedido",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pedido_id = table.Column<Guid>(type: "uuid", nullable: false),
                    artigo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome_artigo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    quantidade = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false, defaultValue: 1.000m),
                    preco_unitario = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0.00m),
                    taxa_iva = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 16.00m),
                    subtotal = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0.00m),
                    total_iva = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0.00m),
                    total_liquido = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0.00m),
                    estacao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    notas = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    anulado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    anulado_motivo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    anulado_por = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    anulado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    criado_por = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    atualizado_por = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    versao = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itens_pedido", x => x.id);
                    table.ForeignKey(
                        name: "FK_itens_pedido_artigos_artigo_id",
                        column: x => x.artigo_id,
                        principalTable: "artigos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_itens_pedido_estacoes_preparacao_estacao_id",
                        column: x => x.estacao_id,
                        principalTable: "estacoes_preparacao",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_itens_pedido_pedidos_pedido_id",
                        column: x => x.pedido_id,
                        principalTable: "pedidos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "itens_pedido_opcoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_pedido_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opcao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome_opcao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    delta_preco = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0.00m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itens_pedido_opcoes", x => x.id);
                    table.ForeignKey(
                        name: "FK_itens_pedido_opcoes_itens_pedido_item_pedido_id",
                        column: x => x.item_pedido_id,
                        principalTable: "itens_pedido",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_itens_pedido_opcoes_opcoes_opcao_id",
                        column: x => x.opcao_id,
                        principalTable: "opcoes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chaves_idempotencia_expira_em",
                table: "chaves_idempotencia",
                column: "expira_em");

            migrationBuilder.CreateIndex(
                name: "IX_contas_estado",
                table: "contas",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "IX_contas_mesa_id",
                table: "contas",
                column: "mesa_id");

            migrationBuilder.CreateIndex(
                name: "IX_contas_numero",
                table: "contas",
                column: "numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_itens_pedido_artigo_id",
                table: "itens_pedido",
                column: "artigo_id");

            migrationBuilder.CreateIndex(
                name: "IX_itens_pedido_estacao_id",
                table: "itens_pedido",
                column: "estacao_id");

            migrationBuilder.CreateIndex(
                name: "IX_itens_pedido_estado",
                table: "itens_pedido",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "IX_itens_pedido_pedido_id",
                table: "itens_pedido",
                column: "pedido_id");

            migrationBuilder.CreateIndex(
                name: "IX_itens_pedido_opcoes_item_pedido_id",
                table: "itens_pedido_opcoes",
                column: "item_pedido_id");

            migrationBuilder.CreateIndex(
                name: "IX_itens_pedido_opcoes_opcao_id",
                table: "itens_pedido_opcoes",
                column: "opcao_id");

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_conta_id",
                table: "pedidos",
                column: "conta_id");

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_estado",
                table: "pedidos",
                column: "estado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chaves_idempotencia");

            migrationBuilder.DropTable(
                name: "itens_pedido_opcoes");

            migrationBuilder.DropTable(
                name: "itens_pedido");

            migrationBuilder.DropTable(
                name: "pedidos");

            migrationBuilder.DropTable(
                name: "contas");
        }
    }
}
