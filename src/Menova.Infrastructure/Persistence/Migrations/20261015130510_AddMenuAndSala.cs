using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Menova.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuAndSala : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alergenios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    icone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alergenios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "estacoes_preparacao",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tem_ecra = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    imprime_talao = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    impressora_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_estacoes_preparacao", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "etiquetas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    cor_hex = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "#6E1A33")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_etiquetas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "grupos_opcoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    obrigatorio = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    min = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    max = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
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
                    table.PrimaryKey("PK_grupos_opcoes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "menu_versao",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    versao = table.Column<long>(type: "bigint", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_versao", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "zonas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
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
                    table.PrimaryKey("PK_zonas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "categorias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    icone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ordem = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    estacao_padrao_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_categorias", x => x.id);
                    table.ForeignKey(
                        name: "FK_categorias_categorias_parent_id",
                        column: x => x.parent_id,
                        principalTable: "categorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_categorias_estacoes_preparacao_estacao_padrao_id",
                        column: x => x.estacao_padrao_id,
                        principalTable: "estacoes_preparacao",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "opcoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    grupo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    delta_preco = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0.00m),
                    ordem = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_opcoes", x => x.id);
                    table.ForeignKey(
                        name: "FK_opcoes_grupos_opcoes_grupo_id",
                        column: x => x.grupo_id,
                        principalTable: "grupos_opcoes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mesas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    zona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    lugares = table.Column<int>(type: "integer", nullable: false, defaultValue: 4),
                    posicao_x = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    posicao_y = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    estado = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_mesas", x => x.id);
                    table.ForeignKey(
                        name: "FK_mesas_zonas_zona_id",
                        column: x => x.zona_id,
                        principalTable: "zonas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "artigos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo_interno = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descricao_curta = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    descricao_longa = table.Column<string>(type: "text", nullable: true),
                    preco = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    taxa_iva = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 16.00m),
                    estacao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tempo_preparacao_min = table.Column<int>(type: "integer", nullable: false, defaultValue: 15),
                    destaque = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    esgotado_manual = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    disponivel = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ordem = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_artigos", x => x.id);
                    table.ForeignKey(
                        name: "FK_artigos_categorias_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_artigos_estacoes_preparacao_estacao_id",
                        column: x => x.estacao_id,
                        principalTable: "estacoes_preparacao",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "contas_mesas",
                columns: table => new
                {
                    conta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mesa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    adicionada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contas_mesas", x => new { x.conta_id, x.mesa_id });
                    table.ForeignKey(
                        name: "FK_contas_mesas_mesas_mesa_id",
                        column: x => x.mesa_id,
                        principalTable: "mesas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "artigos_alergenios",
                columns: table => new
                {
                    artigo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alergenio_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_artigos_alergenios", x => new { x.artigo_id, x.alergenio_id });
                    table.ForeignKey(
                        name: "FK_artigos_alergenios_alergenios_alergenio_id",
                        column: x => x.alergenio_id,
                        principalTable: "alergenios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_artigos_alergenios_artigos_artigo_id",
                        column: x => x.artigo_id,
                        principalTable: "artigos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "artigos_codigos_barras",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    artigo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "EAN13")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_artigos_codigos_barras", x => x.id);
                    table.ForeignKey(
                        name: "FK_artigos_codigos_barras_artigos_artigo_id",
                        column: x => x.artigo_id,
                        principalTable: "artigos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "artigos_etiquetas",
                columns: table => new
                {
                    artigo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    etiqueta_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_artigos_etiquetas", x => new { x.artigo_id, x.etiqueta_id });
                    table.ForeignKey(
                        name: "FK_artigos_etiquetas_artigos_artigo_id",
                        column: x => x.artigo_id,
                        principalTable: "artigos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_artigos_etiquetas_etiquetas_etiqueta_id",
                        column: x => x.etiqueta_id,
                        principalTable: "etiquetas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "artigos_grupos_opcoes",
                columns: table => new
                {
                    artigo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grupo_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_artigos_grupos_opcoes", x => new { x.artigo_id, x.grupo_id });
                    table.ForeignKey(
                        name: "FK_artigos_grupos_opcoes_artigos_artigo_id",
                        column: x => x.artigo_id,
                        principalTable: "artigos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_artigos_grupos_opcoes_grupos_opcoes_grupo_id",
                        column: x => x.grupo_id,
                        principalTable: "grupos_opcoes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "artigos_imagens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    artigo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    caminho_base = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    principal = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    largura = table.Column<int>(type: "integer", nullable: false),
                    altura = table.Column<int>(type: "integer", nullable: false),
                    hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_artigos_imagens", x => x.id);
                    table.ForeignKey(
                        name: "FK_artigos_imagens_artigos_artigo_id",
                        column: x => x.artigo_id,
                        principalTable: "artigos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alergenios_codigo",
                table: "alergenios",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_artigos_categoria_id",
                table: "artigos",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_artigos_codigo_interno",
                table: "artigos",
                column: "codigo_interno",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_artigos_esgotado_manual",
                table: "artigos",
                column: "esgotado_manual");

            migrationBuilder.CreateIndex(
                name: "IX_artigos_estacao_id",
                table: "artigos",
                column: "estacao_id");

            migrationBuilder.CreateIndex(
                name: "IX_artigos_nome",
                table: "artigos",
                column: "nome");

            migrationBuilder.CreateIndex(
                name: "IX_artigos_alergenios_alergenio_id",
                table: "artigos_alergenios",
                column: "alergenio_id");

            migrationBuilder.CreateIndex(
                name: "IX_artigos_codigos_barras_artigo_id",
                table: "artigos_codigos_barras",
                column: "artigo_id");

            migrationBuilder.CreateIndex(
                name: "IX_artigos_codigos_barras_codigo",
                table: "artigos_codigos_barras",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_artigos_etiquetas_etiqueta_id",
                table: "artigos_etiquetas",
                column: "etiqueta_id");

            migrationBuilder.CreateIndex(
                name: "IX_artigos_grupos_opcoes_grupo_id",
                table: "artigos_grupos_opcoes",
                column: "grupo_id");

            migrationBuilder.CreateIndex(
                name: "IX_artigos_imagens_artigo_id",
                table: "artigos_imagens",
                column: "artigo_id");

            migrationBuilder.CreateIndex(
                name: "IX_categorias_estacao_padrao_id",
                table: "categorias",
                column: "estacao_padrao_id");

            migrationBuilder.CreateIndex(
                name: "IX_categorias_parent_id",
                table: "categorias",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_contas_mesas_mesa_id",
                table: "contas_mesas",
                column: "mesa_id");

            migrationBuilder.CreateIndex(
                name: "IX_etiquetas_codigo",
                table: "etiquetas",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mesas_zona_id_numero",
                table: "mesas",
                columns: new[] { "zona_id", "numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_opcoes_grupo_id",
                table: "opcoes",
                column: "grupo_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "artigos_alergenios");

            migrationBuilder.DropTable(
                name: "artigos_codigos_barras");

            migrationBuilder.DropTable(
                name: "artigos_etiquetas");

            migrationBuilder.DropTable(
                name: "artigos_grupos_opcoes");

            migrationBuilder.DropTable(
                name: "artigos_imagens");

            migrationBuilder.DropTable(
                name: "contas_mesas");

            migrationBuilder.DropTable(
                name: "menu_versao");

            migrationBuilder.DropTable(
                name: "opcoes");

            migrationBuilder.DropTable(
                name: "alergenios");

            migrationBuilder.DropTable(
                name: "etiquetas");

            migrationBuilder.DropTable(
                name: "artigos");

            migrationBuilder.DropTable(
                name: "mesas");

            migrationBuilder.DropTable(
                name: "grupos_opcoes");

            migrationBuilder.DropTable(
                name: "categorias");

            migrationBuilder.DropTable(
                name: "zonas");

            migrationBuilder.DropTable(
                name: "estacoes_preparacao");
        }
    }
}
