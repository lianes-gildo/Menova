using Menova.Application.Security;
using Menova.Domain.Configuracao;
using Menova.Domain.Estacoes;
using Menova.Domain.Identity;
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

            // 7. Semear Menu, Sala, Estações e Artigos Padrão
            await SemearMenuESalaAsync(context, logger);
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
}
