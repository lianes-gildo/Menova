using System.Security.Cryptography;
using System.Text;
using Menova.Application.Security;
using Menova.Domain.Caixa;
using Menova.Domain.Configuracao;
using Menova.Domain.Estacoes;
using Menova.Domain.Identity;
using Menova.Domain.Impressao;
using Menova.Domain.Menu;
using Menova.Domain.Sala;
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

            // Admin tem todas as permissões
            await SincronizarPermissoesAsync(context, adminPerfil.Id, todasPermissoes.Select(p => p.Codigo));

            // Gerente tem todas exceto gerir administradores/perfis e restaurar backups
            var permissoesGerente = todasPermissoes
                .Where(p => p.Codigo != PermissoesCatalogo.BackupsRestaurar && p.Codigo != PermissoesCatalogo.PerfisGerir)
                .Select(p => p.Codigo);
            await SincronizarPermissoesAsync(context, gerentePerfil.Id, permissoesGerente);

            // Operador de Caixa: exclusivo operações de caixa, cobrança, emissão Z e consulta de menu/mesas
            var permissoesCaixa = new[]
            {
                PermissoesCatalogo.CaixaAbrir,
                PermissoesCatalogo.CaixaCobrar,
                PermissoesCatalogo.CaixaFechar,
                PermissoesCatalogo.CaixaMovimentos,
                PermissoesCatalogo.CaixaVerTodas,
                PermissoesCatalogo.CaixaReimprimir,
                PermissoesCatalogo.ZVer,
                PermissoesCatalogo.MesasVer,
                PermissoesCatalogo.ContasAbrir,
                PermissoesCatalogo.ContasVerTodas,
                PermissoesCatalogo.ItensAdicionar,
                PermissoesCatalogo.ItensEnviar,
                PermissoesCatalogo.MenuVer
            };
            await SincronizarPermissoesAsync(context, caixaPerfil.Id, permissoesCaixa);

            // Garçom: exclusivo salão, mesas, envio de pedidos e consulta de artigos/esgotados
            var permissoesGarcom = new[]
            {
                PermissoesCatalogo.MesasVer,
                PermissoesCatalogo.ContasAbrir,
                PermissoesCatalogo.ContasVerTodas,
                PermissoesCatalogo.ItensAdicionar,
                PermissoesCatalogo.ItensEnviar,
                PermissoesCatalogo.ItensAnular,
                PermissoesCatalogo.MenuVer,
                PermissoesCatalogo.ArtigosEsgotar
            };
            await SincronizarPermissoesAsync(context, garcomPerfil.Id, permissoesGarcom);

            // Cozinha e Bar: exclusivo ecrã KDS de preparação e controlo de esgotados 86
            var permissoesCozinha = new[]
            {
                PermissoesCatalogo.CozinhaVer,
                PermissoesCatalogo.CozinhaOperar,
                PermissoesCatalogo.ArtigosEsgotar,
                PermissoesCatalogo.MenuVer
            };
            await SincronizarPermissoesAsync(context, cozinhaPerfil.Id, permissoesCozinha);

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

            if (adminUser != null && adminUser.HashPin == null)
            {
                adminUser.HashPin = GerarHashPin("9999");
                await context.SaveChangesAsync();
            }

            // 6.1 Semear Utilizador Garçom Inicial
            var garcomUser = await context.Utilizadores
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.NomeUtilizador.ToLower() == "garcom1");

            if (garcomUser == null)
            {
                garcomUser = new Utilizador
                {
                    NomeUtilizador = "garcom1",
                    NomeCompleto = "Garçom Principal 01",
                    Email = "garcom1@menova.mz",
                    Telefone = "+258 84 222 3333",
                    Foto = "default.jpg",
                    Ativo = true,
                    DeveAlterarPalavraPasse = false,
                    SeloSeguranca = Guid.NewGuid().ToString("N"),
                    HashPin = GerarHashPin("1111")
                };
                garcomUser.HashPalavraPasse = passwordHasher.HashPassword(garcomUser, "Garcom@2026");
                context.Utilizadores.Add(garcomUser);
                await context.SaveChangesAsync();

                context.UtilizadoresPerfis.Add(new UtilizadorPerfil
                {
                    UtilizadorId = garcomUser.Id,
                    PerfilId = garcomPerfil.Id
                });
                await context.SaveChangesAsync();
                logger.LogInformation("Utilizador garcom1 inicial criado com sucesso (PIN: 1111, Palavra-passe: Garcom@2026).");
            }
            else
            {
                garcomUser.HashPin = GerarHashPin("1111");
                await context.SaveChangesAsync();
            }

            // 6.2 Semear Utilizador Caixa Inicial
            var caixaUser = await context.Utilizadores
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.NomeUtilizador.ToLower() == "caixa1");

            if (caixaUser == null)
            {
                caixaUser = new Utilizador
                {
                    NomeUtilizador = "caixa1",
                    NomeCompleto = "Operador de Caixa 01",
                    Email = "caixa1@menova.mz",
                    Telefone = "+258 84 333 4444",
                    Foto = "default.jpg",
                    Ativo = true,
                    DeveAlterarPalavraPasse = false,
                    SeloSeguranca = Guid.NewGuid().ToString("N"),
                    HashPin = GerarHashPin("2222")
                };
                caixaUser.HashPalavraPasse = passwordHasher.HashPassword(caixaUser, "Caixa@2026");
                context.Utilizadores.Add(caixaUser);
                await context.SaveChangesAsync();

                context.UtilizadoresPerfis.Add(new UtilizadorPerfil
                {
                    UtilizadorId = caixaUser.Id,
                    PerfilId = caixaPerfil.Id
                });
                await context.SaveChangesAsync();
                logger.LogInformation("Utilizador caixa1 inicial criado com sucesso (PIN: 2222, Palavra-passe: Caixa@2026).");
            }
            else
            {
                caixaUser.HashPin = GerarHashPin("2222");
                await context.SaveChangesAsync();
            }

            // 6.3 Semear Utilizador Cozinha Inicial
            var cozinhaUser = await context.Utilizadores
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.NomeUtilizador.ToLower() == "cozinha1");

            if (cozinhaUser == null)
            {
                cozinhaUser = new Utilizador
                {
                    NomeUtilizador = "cozinha1",
                    NomeCompleto = "Chefe de Cozinha",
                    Email = "cozinha1@menova.mz",
                    Telefone = "+258 84 444 5555",
                    Foto = "default.jpg",
                    Ativo = true,
                    DeveAlterarPalavraPasse = false,
                    SeloSeguranca = Guid.NewGuid().ToString("N"),
                    HashPin = GerarHashPin("3333")
                };
                cozinhaUser.HashPalavraPasse = passwordHasher.HashPassword(cozinhaUser, "Cozinha@2026");
                context.Utilizadores.Add(cozinhaUser);
                await context.SaveChangesAsync();

                context.UtilizadoresPerfis.Add(new UtilizadorPerfil
                {
                    UtilizadorId = cozinhaUser.Id,
                    PerfilId = cozinhaPerfil.Id
                });
                await context.SaveChangesAsync();
                logger.LogInformation("Utilizador cozinha1 inicial criado com sucesso (PIN: 3333, Palavra-passe: Cozinha@2026).");
            }
            else
            {
                cozinhaUser.HashPin = GerarHashPin("3333");
                await context.SaveChangesAsync();
            }

            // 6.4 Semear Utilizador Gerente Inicial
            var gerenteUser = await context.Utilizadores
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.NomeUtilizador.ToLower() == "gerente1");

            if (gerenteUser == null)
            {
                gerenteUser = new Utilizador
                {
                    NomeUtilizador = "gerente1",
                    NomeCompleto = "Gerente de Operação 01",
                    Email = "gerente1@menova.mz",
                    Telefone = "+258 84 555 6666",
                    Foto = "default.jpg",
                    Ativo = true,
                    DeveAlterarPalavraPasse = false,
                    SeloSeguranca = Guid.NewGuid().ToString("N"),
                    HashPin = GerarHashPin("5555")
                };
                gerenteUser.HashPalavraPasse = passwordHasher.HashPassword(gerenteUser, "Gerente@2026");
                context.Utilizadores.Add(gerenteUser);
                await context.SaveChangesAsync();

                context.UtilizadoresPerfis.Add(new UtilizadorPerfil
                {
                    UtilizadorId = gerenteUser.Id,
                    PerfilId = gerentePerfil.Id
                });
                await context.SaveChangesAsync();
                logger.LogInformation("Utilizador gerente1 inicial criado com sucesso (PIN: 5555, Palavra-passe: Gerente@2026).");
            }
            else
            {
                gerenteUser.HashPin = GerarHashPin("5555");
                await context.SaveChangesAsync();
            }

            // 7. Semear Menu, Sala, Estações e Artigos Padrão
            await SemearMenuESalaAsync(context, logger);

            // 8. Semear Caixa e Impressora Padrão
            await SemearCaixaEImpressoraAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro durante a inicialização / seeding da base de dados.");
            throw;
        }
    }

    private static async Task SincronizarPermissoesAsync(AppDbContext context, Guid perfilId, IEnumerable<string> codigosEsperados)
    {
        var listaEsperada = codigosEsperados.Distinct().ToList();
        var atuais = await context.PerfisPermissoes
            .Where(pp => pp.PerfilId == perfilId)
            .ToListAsync();

        var codigosAtuais = atuais.Select(pp => pp.PermissaoCodigo).ToList();

        // 1. Remover permissões que já não devem pertencer ao perfil
        var paraRemover = atuais.Where(pp => !listaEsperada.Contains(pp.PermissaoCodigo)).ToList();
        if (paraRemover.Any())
        {
            context.PerfisPermissoes.RemoveRange(paraRemover);
        }

        // 2. Inserir novas permissões em falta
        var paraInserir = listaEsperada
            .Except(codigosAtuais)
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
                    RAISE EXCEPTION 'A tabela é imutável (WORM). Operações de UPDATE, DELETE ou TRUNCATE são estritamente proibidas.';
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

                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'documentos_z') THEN
                        IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_bloquear_mutacao_documentos_z') THEN
                            CREATE TRIGGER trg_bloquear_mutacao_documentos_z
                            BEFORE UPDATE OR DELETE OR TRUNCATE ON documentos_z
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

    private static async Task SemearMenuESalaAsync(AppDbContext context, ILogger logger)
    {
        // 1. Estações de Preparação
        var estacaoCozinha = await context.EstacoesPreparacao.FirstOrDefaultAsync(e => e.Nome == "Cozinha");
        if (estacaoCozinha == null)
        {
            estacaoCozinha = new EstacaoPreparacao { Nome = "Cozinha", TemEcra = true, ImprimeTalao = false, Ativa = true };
            context.EstacoesPreparacao.Add(estacaoCozinha);
        }

        var estacaoBar = await context.EstacoesPreparacao.FirstOrDefaultAsync(e => e.Nome == "Bar");
        if (estacaoBar == null)
        {
            estacaoBar = new EstacaoPreparacao { Nome = "Bar", TemEcra = true, ImprimeTalao = false, Ativa = true };
            context.EstacoesPreparacao.Add(estacaoBar);
        }
        await context.SaveChangesAsync();

        // 2. Zonas e Mesas
        var zonaSalao = await context.Zonas.FirstOrDefaultAsync(z => z.Nome == "Salão Principal");
        if (zonaSalao == null)
        {
            zonaSalao = new Zona { Nome = "Salão Principal", Ordem = 1, Ativa = true };
            context.Zonas.Add(zonaSalao);
            await context.SaveChangesAsync();

            for (int i = 1; i <= 6; i++)
            {
                context.Mesas.Add(new Mesa
                {
                    ZonaId = zonaSalao.Id,
                    Numero = i.ToString("D2"),
                    Lugares = (i % 2 == 0) ? 4 : 2,
                    PosicaoX = ((i - 1) % 3) * 100,
                    PosicaoY = ((i - 1) / 3) * 100,
                    Estado = EstadoMesa.Livre,
                    Ativa = true
                });
            }
            await context.SaveChangesAsync();
        }

        var zonaEsplanada = await context.Zonas.FirstOrDefaultAsync(z => z.Nome == "Esplanada");
        if (zonaEsplanada == null)
        {
            zonaEsplanada = new Zona { Nome = "Esplanada", Ordem = 2, Ativa = true };
            context.Zonas.Add(zonaEsplanada);
            await context.SaveChangesAsync();

            for (int i = 7; i <= 10; i++)
            {
                context.Mesas.Add(new Mesa
                {
                    ZonaId = zonaEsplanada.Id,
                    Numero = i.ToString("D2"),
                    Lugares = 4,
                    PosicaoX = ((i - 7) % 2) * 100,
                    PosicaoY = ((i - 7) / 2) * 100,
                    Estado = EstadoMesa.Livre,
                    Ativa = true
                });
            }
            await context.SaveChangesAsync();
        }

        // 3. Alérgenos
        var alergenios = new[]
        {
            ("CRUST", "Crustáceos", "bi-water"),
            ("GLUT", "Glúten", "bi-egg"),
            ("LACT", "Lactose", "bi-cup-hot"),
            ("PEIXE", "Peixe", "bi-water"),
            ("AMEND", "Amendoim", "bi-circle")
        };
        foreach (var (cod, nome, icone) in alergenios)
        {
            if (!await context.Alergenios.AnyAsync(a => a.Codigo == cod))
            {
                context.Alergenios.Add(new Alergenio { Codigo = cod, Nome = nome, Icone = icone });
            }
        }
        await context.SaveChangesAsync();

        // 4. Etiquetas
        var etiquetas = new[]
        {
            ("ESP_MZ", "Especialidade Moçambicana", "#6E1A33"),
            ("VEG", "Vegetariano", "#1E7E34"),
            ("PIC", "Picante", "#D9534F"),
            ("SEM_GLUT", "Sem Glúten", "#0D6EFD")
        };
        foreach (var (cod, nome, cor) in etiquetas)
        {
            if (!await context.Etiquetas.AnyAsync(t => t.Codigo == cod))
            {
                context.Etiquetas.Add(new Etiqueta { Codigo = cod, Nome = nome, CorHex = cor });
            }
        }
        await context.SaveChangesAsync();

        // 5. Categorias
        var catEntradas = await context.Categorias.FirstOrDefaultAsync(c => c.Nome == "Entradas");
        if (catEntradas == null)
        {
            catEntradas = new Categoria { Nome = "Entradas", Icone = "bi-journal-bookmark", Ordem = 1, EstacaoPadraoId = estacaoCozinha.Id, Ativa = true };
            context.Categorias.Add(catEntradas);
        }

        var catPratos = await context.Categorias.FirstOrDefaultAsync(c => c.Nome == "Pratos Principais");
        if (catPratos == null)
        {
            catPratos = new Categoria { Nome = "Pratos Principais", Icone = "bi-cup-hot", Ordem = 2, EstacaoPadraoId = estacaoCozinha.Id, Ativa = true };
            context.Categorias.Add(catPratos);
        }

        var catBebidas = await context.Categorias.FirstOrDefaultAsync(c => c.Nome == "Bebidas");
        if (catBebidas == null)
        {
            catBebidas = new Categoria { Nome = "Bebidas", Icone = "bi-cup-straw", Ordem = 3, EstacaoPadraoId = estacaoBar.Id, Ativa = true };
            context.Categorias.Add(catBebidas);
        }

        var catSobremesas = await context.Categorias.FirstOrDefaultAsync(c => c.Nome == "Sobremesas");
        if (catSobremesas == null)
        {
            catSobremesas = new Categoria { Nome = "Sobremesas", Icone = "bi-cake", Ordem = 4, EstacaoPadraoId = estacaoCozinha.Id, Ativa = true };
            context.Categorias.Add(catSobremesas);
        }
        await context.SaveChangesAsync();

        // 6. Grupos de Opções
        var grupoPonto = await context.GruposOpcoes.FirstOrDefaultAsync(g => g.Nome == "Ponto da Carne");
        if (grupoPonto == null)
        {
            grupoPonto = new GrupoOpcoes { Nome = "Ponto da Carne", Obrigatorio = true, Min = 1, Max = 1 };
            grupoPonto.Opcoes.Add(new Opcao { Nome = "Mal passado", DeltaPreco = 0m, Ordem = 1 });
            grupoPonto.Opcoes.Add(new Opcao { Nome = "Ao ponto", DeltaPreco = 0m, Ordem = 2 });
            grupoPonto.Opcoes.Add(new Opcao { Nome = "Bem passado", DeltaPreco = 0m, Ordem = 3 });
            context.GruposOpcoes.Add(grupoPonto);
            await context.SaveChangesAsync();
        }

        // 7. Artigos do Menu (Moçambique: IVA 16%, Preços MZN)
        if (!await context.Artigos.AnyAsync())
        {
            var artigosSeed = new[]
            {
                new Artigo
                {
                    CategoriaId = catEntradas.Id,
                    CodigoInterno = "ENT-001",
                    Nome = "Chamuças de Carne (4 un)",
                    DescricaoCurta = "Crocantes, recheadas com carne moída temperada com especiarias",
                    Preco = 250.00m,
                    TaxaIva = 16.00m,
                    EstacaoId = estacaoCozinha.Id,
                    TempoPreparacaoMin = 10,
                    Destaque = true,
                    Ativo = true,
                    Disponivel = true
                },
                new Artigo
                {
                    CategoriaId = catEntradas.Id,
                    CodigoInterno = "ENT-002",
                    Nome = "Camarão Grelhado Nacional (Entrada)",
                    DescricaoCurta = "Camarão fresco de Moçambique grelhado ao alho e piripiri",
                    Preco = 650.00m,
                    TaxaIva = 16.00m,
                    EstacaoId = estacaoCozinha.Id,
                    TempoPreparacaoMin = 15,
                    Destaque = true,
                    Ativo = true,
                    Disponivel = true
                },
                new Artigo
                {
                    CategoriaId = catPratos.Id,
                    CodigoInterno = "PRT-001",
                    Nome = "Frango à Zambeziana",
                    DescricaoCurta = "Frango marinado em leite de coco e piripiri, grelhado na brasa com arroz de coco",
                    Preco = 650.00m,
                    TaxaIva = 16.00m,
                    EstacaoId = estacaoCozinha.Id,
                    TempoPreparacaoMin = 25,
                    Destaque = true,
                    Ativo = true,
                    Disponivel = true
                },
                new Artigo
                {
                    CategoriaId = catPratos.Id,
                    CodigoInterno = "PRT-002",
                    Nome = "Matapa Tradicional com Camarão",
                    DescricaoCurta = "Folhas de mandioca com leite de coco, amendoim pilado e camarão",
                    Preco = 750.00m,
                    TaxaIva = 16.00m,
                    EstacaoId = estacaoCozinha.Id,
                    TempoPreparacaoMin = 20,
                    Destaque = true,
                    Ativo = true,
                    Disponivel = true
                },
                new Artigo
                {
                    CategoriaId = catPratos.Id,
                    CodigoInterno = "PRT-003",
                    Nome = "Bife à Menova com Batatas Fritas",
                    DescricaoCurta = "Lombo de novilho grelhado com molho especial de natas e pimenta verde",
                    Preco = 850.00m,
                    TaxaIva = 16.00m,
                    EstacaoId = estacaoCozinha.Id,
                    TempoPreparacaoMin = 20,
                    Destaque = false,
                    Ativo = true,
                    Disponivel = true
                },
                new Artigo
                {
                    CategoriaId = catBebidas.Id,
                    CodigoInterno = "BEB-001",
                    Nome = "Cerveja Laurentina Preta (330ml)",
                    DescricaoCurta = "A lendária cerveja preta premiada de Moçambique",
                    Preco = 130.00m,
                    TaxaIva = 16.00m,
                    EstacaoId = estacaoBar.Id,
                    TempoPreparacaoMin = 3,
                    Destaque = true,
                    Ativo = true,
                    Disponivel = true
                },
                new Artigo
                {
                    CategoriaId = catBebidas.Id,
                    CodigoInterno = "BEB-002",
                    Nome = "Cerveja 2M (330ml)",
                    DescricaoCurta = "Cerveja nacional moçambicana bem fresca",
                    Preco = 120.00m,
                    TaxaIva = 16.00m,
                    EstacaoId = estacaoBar.Id,
                    TempoPreparacaoMin = 3,
                    Destaque = false,
                    Ativo = true,
                    Disponivel = true
                },
                new Artigo
                {
                    CategoriaId = catBebidas.Id,
                    CodigoInterno = "BEB-003",
                    Nome = "Água Mineral da Namaacha (500ml)",
                    DescricaoCurta = "Água mineral natural sem gás da Namaacha",
                    Preco = 60.00m,
                    TaxaIva = 16.00m,
                    EstacaoId = estacaoBar.Id,
                    TempoPreparacaoMin = 2,
                    Destaque = false,
                    Ativo = true,
                    Disponivel = true
                },
                new Artigo
                {
                    CategoriaId = catBebidas.Id,
                    CodigoInterno = "BEB-004",
                    Nome = "Sumo Natural de Maracujá",
                    DescricaoCurta = "Sumo natural preparado na hora",
                    Preco = 150.00m,
                    TaxaIva = 16.00m,
                    EstacaoId = estacaoBar.Id,
                    TempoPreparacaoMin = 5,
                    Destaque = false,
                    Ativo = true,
                    Disponivel = true
                },
                new Artigo
                {
                    CategoriaId = catSobremesas.Id,
                    CodigoInterno = "SOB-001",
                    Nome = "Pudim de Leite Condensado",
                    DescricaoCurta = "Receita caseira tradicional com calda de caramelo dourado",
                    Preco = 220.00m,
                    TaxaIva = 16.00m,
                    EstacaoId = estacaoCozinha.Id,
                    TempoPreparacaoMin = 5,
                    Destaque = true,
                    Ativo = true,
                    Disponivel = true
                }
            };

            context.Artigos.AddRange(artigosSeed);
            await context.SaveChangesAsync();

            var bife = await context.Artigos.FirstOrDefaultAsync(a => a.CodigoInterno == "PRT-003");
            if (bife != null)
            {
                context.ArtigosGruposOpcoes.Add(new ArtigoGrupoOpcoes
                {
                    ArtigoId = bife.Id,
                    GrupoId = grupoPonto.Id
                });
                await context.SaveChangesAsync();
            }
        }

        // 8. Menu Versão
        var menuVersao = await context.MenuVersao.FirstOrDefaultAsync(v => v.Id == 1);
        if (menuVersao == null)
        {
            context.MenuVersao.Add(new MenuVersao
            {
                Id = 1,
                Versao = 1,
                AtualizadoEm = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        logger.LogInformation("Menu, Sala, Estações e Artigos moçambicanos semeados com sucesso.");
    }

    private static async Task SemearCaixaEImpressoraAsync(AppDbContext context, ILogger logger)
    {
        if (!await context.Caixas.AnyAsync())
        {
            context.Caixas.Add(new Caixa
            {
                Nome = "Caixa 01 - Balcão Principal",
                Numero = 1,
                Ativa = true
            });
            await context.SaveChangesAsync();
        }

        if (!await context.Impressoras.AnyAsync())
        {
            context.Impressoras.Add(new Impressora
            {
                Nome = "Impressora Térmica Balcão (ESC/POS)",
                EnderecoIp = "192.168.1.200",
                Porta = 9100,
                LarguraColunas = 42,
                Ativa = true,
                CortarPapel = true,
                AbrirGaveta = false
            });
            await context.SaveChangesAsync();
        }

        logger.LogInformation("Caixa padrão e Impressora padrão semeados com sucesso.");
    }

    private static string GerarHashPin(string pin, string pepper = "Menova_Dev_Pin_Pepper_2026_Africa_Maputo")
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        var salt = Convert.ToHexString(bytes).ToLowerInvariant();
        var input = $"{pin}:{salt}:{pepper}";
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(input),
            Encoding.UTF8.GetBytes(salt),
            iterations: 100_000,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: 32);
        return $"{salt}:{Convert.ToHexString(hashBytes).ToLowerInvariant()}";
    }
}
