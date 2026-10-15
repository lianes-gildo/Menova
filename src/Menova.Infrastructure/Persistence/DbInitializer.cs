using Menova.Application.Security;
using Menova.Domain.Configuracao;
using Menova.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Menova.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InicializarAsync(
        AppDbContext context,
        IPasswordHasher<Utilizador> passwordHasher,
        ILogger logger)
    {
        try
        {
            // 1. Criar Triggers WORM de Auditoria no PostgreSQL se a tabela existir
            await ConfigurarTriggersPostgresAsync(context, logger);

            // 2. Semear Permissões
            foreach (var (codigo, modulo, descricao) in PermissoesCatalogo.Todas)
            {
                var permissaoExistente = await context.Permissoes.FindAsync(codigo);
                if (permissaoExistente == null)
                {
                    context.Permissoes.Add(new Permissao
                    {
                        Codigo = codigo,
                        Modulo = modulo,
                        Descricao = descricao
                    });
                }
            }
            await context.SaveChangesAsync();

            // 3. Semear Perfis de Sistema
            var perfisBase = new[]
            {
                (PerfisSistema.Administrador, "Administrador", "Controlo total da plataforma e definições"),
                (PerfisSistema.Gerente, "Gerente", "Supervisão da operação, menu, relatórios e autorizações"),
                (PerfisSistema.Caixa, "Operador de Caixa", "Gestão de caixa, cobranças e fechos"),
                (PerfisSistema.Garcom, "Garçom", "Atendimento em mesas e envio de comandas"),
                (PerfisSistema.CozinhaBar, "Cozinha e Bar", "Ecrã KDS de preparação e controlo de esgotados")
            };

            foreach (var (codigo, nome, desc) in perfisBase)
            {
                var perfil = await context.Perfis.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Codigo == codigo);
                if (perfil == null)
                {
                    perfil = new Perfil
                    {
                        Codigo = codigo,
                        Nome = nome,
                        Descricao = desc,
                        ESistema = true,
                        Ativo = true
                    };
                    context.Perfis.Add(perfil);
                    await context.SaveChangesAsync();
                }
            }

            // 4. Mapear Permissões aos Perfis
            var adminPerfil = await context.Perfis.FirstAsync(p => p.Codigo == PerfisSistema.Administrador);
            var gerentePerfil = await context.Perfis.FirstAsync(p => p.Codigo == PerfisSistema.Gerente);
            var caixaPerfil = await context.Perfis.FirstAsync(p => p.Codigo == PerfisSistema.Caixa);
            var garcomPerfil = await context.Perfis.FirstAsync(p => p.Codigo == PerfisSistema.Garcom);
            var cozinhaPerfil = await context.Perfis.FirstAsync(p => p.Codigo == PerfisSistema.CozinhaBar);

            var todasPermissoes = await context.Permissoes.ToListAsync();

            // Admin tem todas
            await AtribuirPermissoesAsync(context, adminPerfil.Id, todasPermissoes.Select(p => p.Codigo));

            // Gerente tem todas exceto gerir administradores e restaurar backups
            var permissoesGerente = todasPermissoes
                .Where(p => p.Codigo != PermissoesCatalogo.BackupsRestaurar && p.Codigo != PermissoesCatalogo.PerfisGerir)
                .Select(p => p.Codigo);
            await AtribuirPermissoesAsync(context, gerentePerfil.Id, permissoesGerente);

            // Caixa
            var permissoesCaixa = new[]
            {
                PermissoesCatalogo.PainelVer,
                PermissoesCatalogo.CaixaAbrir,
                PermissoesCatalogo.CaixaCobrar,
                PermissoesCatalogo.CaixaFechar,
                PermissoesCatalogo.CaixaMovimentos,
                PermissoesCatalogo.CaixaReimprimir,
                PermissoesCatalogo.ZVer,
                PermissoesCatalogo.MesasVer,
                PermissoesCatalogo.MenuVer
            };
            await AtribuirPermissoesAsync(context, caixaPerfil.Id, permissoesCaixa);

            // Garçom
            var permissoesGarcom = new[]
            {
                PermissoesCatalogo.MesasVer,
                PermissoesCatalogo.ContasAbrir,
                PermissoesCatalogo.ItensAdicionar,
                PermissoesCatalogo.ItensEnviar,
                PermissoesCatalogo.MenuVer
            };
            await AtribuirPermissoesAsync(context, garcomPerfil.Id, permissoesGarcom);

            // Cozinha
            var permissoesCozinha = new[]
            {
                PermissoesCatalogo.CozinhaVer,
                PermissoesCatalogo.CozinhaOperar,
                PermissoesCatalogo.ArtigosEsgotar
            };
            await AtribuirPermissoesAsync(context, cozinhaPerfil.Id, permissoesCozinha);

            // 5. Semear Restaurante Único (Padrão Moçambique)
            var restaurante = await context.Restaurante.FirstOrDefaultAsync();
            if (restaurante == null)
            {
                context.Restaurante.Add(new Restaurante
                {
                    Nome = "Menova Restaurante",
                    Nuit = "100000000", // 9 dígitos Moçambique
                    Morada = "Av. Julius Nyerere, Maputo, Moçambique",
                    Telefone = "+258 84 000 0000",
                    Email = "contacto@menova.mz",
                    RodapeTalao = "Obrigado pela preferência! Volte sempre.",
                    Moeda = "MZN",
                    CasasDecimais = 2,
                    FusoHorario = "Africa/Maputo",
                    TaxaIvaPadrao = 16.00m
                });
                await context.SaveChangesAsync();
            }

            // 6. Semear Administrador Inicial
            var adminUser = await context.Utilizadores
                .IgnoreQueryFilters()
                .Include(u => u.UtilizadoresPerfis)
                .FirstOrDefaultAsync(u => u.NomeUtilizador.ToLower() == "admin");

            if (adminUser == null)
            {
                adminUser = new Utilizador
                {
                    NomeUtilizador = "admin",
                    NomeCompleto = "Administrador do Sistema",
                    Email = "admin@menova.mz",
                    Telefone = "+258 84 111 2222",
                    Foto = "default.jpg",
                    Ativo = true,
                    DeveAlterarPalavraPasse = true, // Obrigatório alterar no primeiro login
                    SeloSeguranca = Guid.NewGuid().ToString("N")
                };

                adminUser.HashPalavraPasse = passwordHasher.HashPassword(adminUser, "MenovaAdmin@2026");
                context.Utilizadores.Add(adminUser);
                await context.SaveChangesAsync();

                context.UtilizadoresPerfis.Add(new UtilizadorPerfil
                {
                    UtilizadorId = adminUser.Id,
                    PerfilId = adminPerfil.Id
                });
                await context.SaveChangesAsync();

                logger.LogInformation("Utilizador administrador inicial criado com sucesso (Utilizador: admin, Palavra-passe temporária: MenovaAdmin@2026).");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro durante a inicialização / seeding da base de dados.");
            throw;
        }
    }

    private static async Task AtribuirPermissoesAsync(AppDbContext context, Guid perfilId, IEnumerable<string> codigos)
    {
        var atuais = await context.PerfisPermissoes
            .Where(pp => pp.PerfilId == perfilId)
            .Select(pp => pp.PermissaoCodigo)
            .ToListAsync();

        var paraInserir = codigos
            .Distinct()
            .Except(atuais)
            .Select(cod => new PerfilPermissao
            {
                PerfilId = perfilId,
                PermissaoCodigo = cod
            });

        context.PerfisPermissoes.AddRange(paraInserir);
        await context.SaveChangesAsync();
    }

    private static async Task ConfigurarTriggersPostgresAsync(AppDbContext context, ILogger logger)
    {
        if (!context.Database.IsNpgsql()) return;

        try
        {
            const string sql = """
                CREATE OR REPLACE FUNCTION fn_bloquear_mutacao_auditoria()
                RETURNS TRIGGER AS $$
                BEGIN
                    RAISE EXCEPTION 'A tabela de auditoria é imutável (WORM). Operações de UPDATE, DELETE ou TRUNCATE são estritamente proibidas.';
                END;
                $$ LANGUAGE plpgsql;

                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'auditoria') THEN
                        IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_bloquear_mutacao_auditoria') THEN
                            CREATE TRIGGER trg_bloquear_mutacao_auditoria
                            BEFORE UPDATE OR DELETE OR TRUNCATE ON auditoria
                            FOR EACH STATEMENT EXECUTE FUNCTION fn_bloquear_mutacao_auditoria();
                        END IF;
                    END IF;
                END $$;
                """;

            await context.Database.ExecuteSqlRawAsync(sql);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Aviso ao configurar trigger WORM de auditoria.");
        }
    }
}
