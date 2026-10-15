using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Menova.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialIdentityAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.CreateTable(
                name: "auditoria",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    data_hora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    utilizador_id = table.Column<Guid>(type: "uuid", nullable: true),
                    nome_utilizador_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    perfil = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    dispositivo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ip = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    acao = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    entidade = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    entidade_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    caixa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sessao_caixa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    valores_antes = table.Column<string>(type: "jsonb", nullable: true),
                    valores_depois = table.Column<string>(type: "jsonb", nullable: true),
                    motivo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    correlacao_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    hash_anterior = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auditoria", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "configuracoes",
                columns: table => new
                {
                    chave = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    valor = table.Column<string>(type: "text", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "string"),
                    descricao = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracoes", x => x.chave);
                });

            migrationBuilder.CreateTable(
                name: "dispositivos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    autorizado_por = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    autorizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ultimo_acesso_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ultimo_ip = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
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
                    table.PrimaryKey("PK_dispositivos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "perfis",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descricao = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    e_sistema = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
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
                    table.PrimaryKey("PK_perfis", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permissoes",
                columns: table => new
                {
                    codigo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    modulo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    descricao = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissoes", x => x.codigo);
                });

            migrationBuilder.CreateTable(
                name: "restaurante",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    nuit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    morada = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    telefone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    rodape_talao = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    moeda = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "MZN"),
                    casas_decimais = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    fuso_horario = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Africa/Maputo"),
                    taxa_iva_padrao = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 16.00m),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    criado_por = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    atualizado_por = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    versao = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_restaurante", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sequencias",
                columns: table => new
                {
                    serie = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ano = table.Column<int>(type: "integer", nullable: false),
                    ultimo_numero = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sequencias", x => new { x.serie, x.ano });
                });

            migrationBuilder.CreateTable(
                name: "utilizadores",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome_completo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    nome_utilizador = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    telefone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    hash_palavra_passe = table.Column<string>(type: "text", nullable: false),
                    hash_pin = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    foto = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "default.jpg"),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ultimo_login_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tentativas_falhadas = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    bloqueado_ate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deve_alterar_palavra_passe = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    selo_seguranca = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
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
                    table.PrimaryKey("PK_utilizadores", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "perfis_permissoes",
                columns: table => new
                {
                    perfil_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permissao_codigo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_perfis_permissoes", x => new { x.perfil_id, x.permissao_codigo });
                    table.ForeignKey(
                        name: "FK_perfis_permissoes_perfis_perfil_id",
                        column: x => x.perfil_id,
                        principalTable: "perfis",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_perfis_permissoes_permissoes_permissao_codigo",
                        column: x => x.permissao_codigo,
                        principalTable: "permissoes",
                        principalColumn: "codigo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sessoes_utilizador",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    utilizador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dispositivo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ultimo_acesso = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ip = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    revogada = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sessoes_utilizador", x => x.id);
                    table.ForeignKey(
                        name: "FK_sessoes_utilizador_dispositivos_dispositivo_id",
                        column: x => x.dispositivo_id,
                        principalTable: "dispositivos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_sessoes_utilizador_utilizadores_utilizador_id",
                        column: x => x.utilizador_id,
                        principalTable: "utilizadores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tentativas_login",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    utilizador_id = table.Column<Guid>(type: "uuid", nullable: true),
                    nome_tentado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ip = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    dispositivo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sucesso = table.Column<bool>(type: "boolean", nullable: false),
                    motivo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    data_hora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tentativas_login", x => x.id);
                    table.ForeignKey(
                        name: "FK_tentativas_login_dispositivos_dispositivo_id",
                        column: x => x.dispositivo_id,
                        principalTable: "dispositivos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tentativas_login_utilizadores_utilizador_id",
                        column: x => x.utilizador_id,
                        principalTable: "utilizadores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "utilizadores_perfis",
                columns: table => new
                {
                    utilizador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    perfil_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_utilizadores_perfis", x => new { x.utilizador_id, x.perfil_id });
                    table.ForeignKey(
                        name: "FK_utilizadores_perfis_perfis_perfil_id",
                        column: x => x.perfil_id,
                        principalTable: "perfis",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_utilizadores_perfis_utilizadores_utilizador_id",
                        column: x => x.utilizador_id,
                        principalTable: "utilizadores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_acao",
                table: "auditoria",
                column: "acao");

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_correlacao_id",
                table: "auditoria",
                column: "correlacao_id");

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_data_hora",
                table: "auditoria",
                column: "data_hora");

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_utilizador_id",
                table: "auditoria",
                column: "utilizador_id");

            migrationBuilder.CreateIndex(
                name: "IX_dispositivos_token_hash",
                table: "dispositivos",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_perfis_codigo",
                table: "perfis",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_perfis_permissoes_permissao_codigo",
                table: "perfis_permissoes",
                column: "permissao_codigo");

            migrationBuilder.CreateIndex(
                name: "IX_sessoes_utilizador_dispositivo_id",
                table: "sessoes_utilizador",
                column: "dispositivo_id");

            migrationBuilder.CreateIndex(
                name: "IX_sessoes_utilizador_utilizador_id",
                table: "sessoes_utilizador",
                column: "utilizador_id");

            migrationBuilder.CreateIndex(
                name: "IX_tentativas_login_data_hora",
                table: "tentativas_login",
                column: "data_hora");

            migrationBuilder.CreateIndex(
                name: "IX_tentativas_login_dispositivo_id",
                table: "tentativas_login",
                column: "dispositivo_id");

            migrationBuilder.CreateIndex(
                name: "IX_tentativas_login_nome_tentado_data_hora",
                table: "tentativas_login",
                columns: new[] { "nome_tentado", "data_hora" });

            migrationBuilder.CreateIndex(
                name: "IX_tentativas_login_utilizador_id",
                table: "tentativas_login",
                column: "utilizador_id");

            migrationBuilder.CreateIndex(
                name: "IX_utilizadores_nome_utilizador",
                table: "utilizadores",
                column: "nome_utilizador",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_utilizadores_perfis_perfil_id",
                table: "utilizadores_perfis",
                column: "perfil_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auditoria");

            migrationBuilder.DropTable(
                name: "configuracoes");

            migrationBuilder.DropTable(
                name: "perfis_permissoes");

            migrationBuilder.DropTable(
                name: "restaurante");

            migrationBuilder.DropTable(
                name: "sequencias");

            migrationBuilder.DropTable(
                name: "sessoes_utilizador");

            migrationBuilder.DropTable(
                name: "tentativas_login");

            migrationBuilder.DropTable(
                name: "utilizadores_perfis");

            migrationBuilder.DropTable(
                name: "permissoes");

            migrationBuilder.DropTable(
                name: "dispositivos");

            migrationBuilder.DropTable(
                name: "perfis");

            migrationBuilder.DropTable(
                name: "utilizadores");
        }
    }
}
