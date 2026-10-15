using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Menova.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCaixaAndImpressao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "caixas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    numero = table.Column<int>(type: "integer", nullable: false),
                    ativa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    criado_por = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    atualizado_por = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    versao = table.Column<long>(type: "bigint", nullable: false),
                    esta_eliminado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    eliminado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    eliminado_por = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_caixas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "impressoras",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    endereco_ip = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    porta = table.Column<int>(type: "integer", nullable: false, defaultValue: 9100),
                    largura_colunas = table.Column<int>(type: "integer", nullable: false, defaultValue: 42),
                    ativa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    cortar_papel = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    abrir_gaveta = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    criado_por = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    atualizado_por = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    versao = table.Column<long>(type: "bigint", nullable: false),
                    esta_eliminado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    eliminado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    eliminado_por = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_impressoras", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sessoes_caixa",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    caixa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    utilizador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aberta_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fechada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    saldo_inicial = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0.00m),
                    total_vendas = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0.00m),
                    total_entradas = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0.00m),
                    total_saidas = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0.00m),
                    saldo_esperado = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0.00m),
                    saldo_contado = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    diferenca = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    observacoes = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sessoes_caixa", x => x.id);
                    table.ForeignKey(
                        name: "FK_sessoes_caixa_caixas_caixa_id",
                        column: x => x.caixa_id,
                        principalTable: "caixas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sessoes_caixa_utilizadores_utilizador_id",
                        column: x => x.utilizador_id,
                        principalTable: "utilizadores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "trabalhos_impressao",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    impressora_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tipo_documento = table.Column<int>(type: "integer", nullable: false),
                    conteudo_bytes = table.Column<byte[]>(type: "bytea", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    tentativas = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    mensagem_erro = table.Column<string>(type: "text", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    enviado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trabalhos_impressao", x => x.id);
                    table.ForeignKey(
                        name: "FK_trabalhos_impressao_impressoras_impressora_id",
                        column: x => x.impressora_id,
                        principalTable: "impressoras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "documentos_z",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sessao_caixa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero_z = table.Column<int>(type: "integer", nullable: false),
                    data_hora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total_bruto = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_desconto = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_iva = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_liquido = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    totais_por_metodo = table.Column<string>(type: "jsonb", nullable: false),
                    totais_por_taxa_iva = table.Column<string>(type: "jsonb", nullable: false),
                    total_documentos_faturados = table.Column<int>(type: "integer", nullable: false),
                    emitido_por = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    hash_anterior = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documentos_z", x => x.id);
                    table.ForeignKey(
                        name: "FK_documentos_z_sessoes_caixa_sessao_caixa_id",
                        column: x => x.sessao_caixa_id,
                        principalTable: "sessoes_caixa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movimentos_caixa",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sessao_caixa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    motivo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    criado_por = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimentos_caixa", x => x.id);
                    table.ForeignKey(
                        name: "FK_movimentos_caixa_sessoes_caixa_sessao_caixa_id",
                        column: x => x.sessao_caixa_id,
                        principalTable: "sessoes_caixa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pagamentos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sessao_caixa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metodo = table.Column<int>(type: "integer", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    valor_recebido = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    troco = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    gorjeta = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    referencia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    criado_por = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagamentos", x => x.id);
                    table.ForeignKey(
                        name: "FK_pagamentos_contas_conta_id",
                        column: x => x.conta_id,
                        principalTable: "contas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pagamentos_sessoes_caixa_sessao_caixa_id",
                        column: x => x.sessao_caixa_id,
                        principalTable: "sessoes_caixa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_documentos_z_data_hora",
                table: "documentos_z",
                column: "data_hora");

            migrationBuilder.CreateIndex(
                name: "IX_documentos_z_numero_z",
                table: "documentos_z",
                column: "numero_z",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_documentos_z_sessao_caixa_id",
                table: "documentos_z",
                column: "sessao_caixa_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimentos_caixa_sessao_caixa_id",
                table: "movimentos_caixa",
                column: "sessao_caixa_id");

            migrationBuilder.CreateIndex(
                name: "IX_pagamentos_conta_id",
                table: "pagamentos",
                column: "conta_id");

            migrationBuilder.CreateIndex(
                name: "IX_pagamentos_sessao_caixa_id",
                table: "pagamentos",
                column: "sessao_caixa_id");

            migrationBuilder.CreateIndex(
                name: "IX_sessoes_caixa_aberta_em",
                table: "sessoes_caixa",
                column: "aberta_em");

            migrationBuilder.CreateIndex(
                name: "IX_sessoes_caixa_caixa_id",
                table: "sessoes_caixa",
                column: "caixa_id");

            migrationBuilder.CreateIndex(
                name: "IX_sessoes_caixa_utilizador_id",
                table: "sessoes_caixa",
                column: "utilizador_id");

            migrationBuilder.CreateIndex(
                name: "IX_trabalhos_impressao_criado_em",
                table: "trabalhos_impressao",
                column: "criado_em");

            migrationBuilder.CreateIndex(
                name: "IX_trabalhos_impressao_estado",
                table: "trabalhos_impressao",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "IX_trabalhos_impressao_impressora_id",
                table: "trabalhos_impressao",
                column: "impressora_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "documentos_z");

            migrationBuilder.DropTable(
                name: "movimentos_caixa");

            migrationBuilder.DropTable(
                name: "pagamentos");

            migrationBuilder.DropTable(
                name: "trabalhos_impressao");

            migrationBuilder.DropTable(
                name: "sessoes_caixa");

            migrationBuilder.DropTable(
                name: "impressoras");

            migrationBuilder.DropTable(
                name: "caixas");
        }
    }
}
