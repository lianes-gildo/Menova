using System.Text.RegularExpressions;
using Menova.Application.Interfaces;
using Menova.Domain.Auditoria;
using Menova.Domain.Common;
using Menova.Domain.Configuracao;
using Menova.Domain.Estacoes;
using Menova.Domain.Identity;
using Menova.Domain.Menu;
using Menova.Domain.Sala;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Menova.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Utilizador> Utilizadores => Set<Utilizador>();
    public DbSet<Perfil> Perfis => Set<Perfil>();
    public DbSet<Permissao> Permissoes => Set<Permissao>();
    public DbSet<PerfilPermissao> PerfisPermissoes => Set<PerfilPermissao>();
    public DbSet<UtilizadorPerfil> UtilizadoresPerfis => Set<UtilizadorPerfil>();
    public DbSet<Dispositivo> Dispositivos => Set<Dispositivo>();
    public DbSet<SessaoUtilizador> SessoesUtilizador => Set<SessaoUtilizador>();
    public DbSet<TentativaLogin> TentativasLogin => Set<TentativaLogin>();
    public DbSet<Restaurante> Restaurante => Set<Restaurante>();
    public DbSet<Configuracao> Configuracoes => Set<Configuracao>();
    public DbSet<Sequencia> Sequencias => Set<Sequencia>();
    public DbSet<RegistoAuditoria> Auditoria => Set<RegistoAuditoria>();

    // Sala e Mesas
    public DbSet<Zona> Zonas => Set<Zona>();
    public DbSet<Mesa> Mesas => Set<Mesa>();
    public DbSet<ContaMesa> ContasMesas => Set<ContaMesa>();

    // Estações de Preparação
    public DbSet<EstacaoPreparacao> EstacoesPreparacao => Set<EstacaoPreparacao>();

    // Menu e Artigos
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Artigo> Artigos => Set<Artigo>();
    public DbSet<ArtigoCodigoBarras> ArtigosCodigosBarras => Set<ArtigoCodigoBarras>();
    public DbSet<ArtigoImagem> ArtigosImagens => Set<ArtigoImagem>();
    public DbSet<Alergenio> Alergenios => Set<Alergenio>();
    public DbSet<ArtigoAlergenio> ArtigosAlergenios => Set<ArtigoAlergenio>();
    public DbSet<Etiqueta> Etiquetas => Set<Etiqueta>();
    public DbSet<ArtigoEtiqueta> ArtigosEtiquetas => Set<ArtigoEtiqueta>();
    public DbSet<GrupoOpcoes> GruposOpcoes => Set<GrupoOpcoes>();
    public DbSet<Opcao> Opcoes => Set<Opcao>();
    public DbSet<ArtigoGrupoOpcoes> ArtigosGruposOpcoes => Set<ArtigoGrupoOpcoes>();
    public DbSet<MenuVersao> MenuVersao => Set<MenuVersao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Extensões PostgreSQL
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.HasPostgresExtension("pg_trgm");

        // 1. Utilizador
        modelBuilder.Entity<Utilizador>(entity =>
        {
            entity.ToTable("utilizadores");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.NomeCompleto).HasMaxLength(150).IsRequired().HasColumnName("nome_completo");
            entity.Property(e => e.NomeUtilizador).HasMaxLength(50).IsRequired().HasColumnName("nome_utilizador");
            entity.HasIndex(e => e.NomeUtilizador).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(100).HasColumnName("email");
            entity.Property(e => e.Telefone).HasMaxLength(30).HasColumnName("telefone");
            entity.Property(e => e.HashPalavraPasse).IsRequired().HasColumnName("hash_palavra_passe");
            entity.Property(e => e.HashPin).HasMaxLength(256).HasColumnName("hash_pin");
            entity.Property(e => e.Foto).HasMaxLength(255).HasDefaultValue("default.jpg").HasColumnName("foto");
            entity.Property(e => e.Ativo).HasDefaultValue(true).HasColumnName("ativo");
            entity.Property(e => e.UltimoLoginEm).HasColumnName("ultimo_login_em");
            entity.Property(e => e.TentativasFalhadas).HasDefaultValue(0).HasColumnName("tentativas_falhadas");
            entity.Property(e => e.BloqueadoAte).HasColumnName("bloqueado_ate");
            entity.Property(e => e.DeveAlterarPalavraPasse).HasDefaultValue(false).HasColumnName("deve_alterar_palavra_passe");
            entity.Property(e => e.SeloSeguranca).HasMaxLength(64).IsRequired().HasColumnName("selo_seguranca");

            entity.Property(e => e.CriadoEm).HasColumnName("criado_em");
            entity.Property(e => e.CriadoPor).HasMaxLength(50).HasColumnName("criado_por");
            entity.Property(e => e.AtualizadoEm).HasColumnName("atualizado_em");
            entity.Property(e => e.AtualizadoPor).HasMaxLength(50).HasColumnName("atualizado_por");
            entity.Property(e => e.Versao).IsConcurrencyToken().HasColumnName("versao");
            entity.Property(e => e.EstaEliminado).HasDefaultValue(false).HasColumnName("esta_eliminado");
            entity.Property(e => e.EliminadoEm).HasColumnName("eliminado_em");
            entity.Property(e => e.EliminadoPor).HasMaxLength(50).HasColumnName("eliminado_por");

            entity.HasQueryFilter(e => !e.EstaEliminado);
        });

        // 2. Perfil
        modelBuilder.Entity<Perfil>(entity =>
        {
            entity.ToTable("perfis");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Codigo).HasMaxLength(30).IsRequired().HasColumnName("codigo");
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired().HasColumnName("nome");
            entity.Property(e => e.Descricao).HasMaxLength(255).HasColumnName("descricao");
            entity.Property(e => e.ESistema).HasDefaultValue(false).HasColumnName("e_sistema");
            entity.Property(e => e.Ativo).HasDefaultValue(true).HasColumnName("ativo");

            entity.Property(e => e.CriadoEm).HasColumnName("criado_em");
            entity.Property(e => e.CriadoPor).HasMaxLength(50).HasColumnName("criado_por");
            entity.Property(e => e.AtualizadoEm).HasColumnName("atualizado_em");
            entity.Property(e => e.AtualizadoPor).HasMaxLength(50).HasColumnName("atualizado_por");
            entity.Property(e => e.Versao).IsConcurrencyToken().HasColumnName("versao");
            entity.Property(e => e.EstaEliminado).HasDefaultValue(false).HasColumnName("esta_eliminado");
            entity.Property(e => e.EliminadoEm).HasColumnName("eliminado_em");
            entity.Property(e => e.EliminadoPor).HasMaxLength(50).HasColumnName("eliminado_por");

            entity.HasQueryFilter(e => !e.EstaEliminado);
        });

        // 3. Permissão
        modelBuilder.Entity<Permissao>(entity =>
        {
            entity.ToTable("permissoes");
            entity.HasKey(e => e.Codigo);
            entity.Property(e => e.Codigo).HasMaxLength(80).HasColumnName("codigo");
            entity.Property(e => e.Modulo).HasMaxLength(50).IsRequired().HasColumnName("modulo");
            entity.Property(e => e.Descricao).HasMaxLength(255).IsRequired().HasColumnName("descricao");
        });

        // 4. PerfilPermissao
        modelBuilder.Entity<PerfilPermissao>(entity =>
        {
            entity.ToTable("perfis_permissoes");
            entity.HasKey(e => new { e.PerfilId, e.PermissaoCodigo });
            entity.Property(e => e.PerfilId).HasColumnName("perfil_id");
            entity.Property(e => e.PermissaoCodigo).HasMaxLength(80).HasColumnName("permissao_codigo");

            entity.HasOne(e => e.Perfil)
                  .WithMany(p => p.PerfisPermissoes)
                  .HasForeignKey(e => e.PerfilId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Permissao)
                  .WithMany(p => p.PerfisPermissoes)
                  .HasForeignKey(e => e.PermissaoCodigo)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.Perfil.EstaEliminado);
        });

        // 5. UtilizadorPerfil
        modelBuilder.Entity<UtilizadorPerfil>(entity =>
        {
            entity.ToTable("utilizadores_perfis");
            entity.HasKey(e => new { e.UtilizadorId, e.PerfilId });
            entity.Property(e => e.UtilizadorId).HasColumnName("utilizador_id");
            entity.Property(e => e.PerfilId).HasColumnName("perfil_id");

            entity.HasOne(e => e.Utilizador)
                  .WithMany(u => u.UtilizadoresPerfis)
                  .HasForeignKey(e => e.UtilizadorId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Perfil)
                  .WithMany(p => p.UtilizadoresPerfis)
                  .HasForeignKey(e => e.PerfilId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => !e.Utilizador.EstaEliminado && !e.Perfil.EstaEliminado);
        });

        // 6. Dispositivo
        modelBuilder.Entity<Dispositivo>(entity =>
        {
            entity.ToTable("dispositivos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired().HasColumnName("nome");
            entity.Property(e => e.Tipo).HasConversion<int>().HasColumnName("tipo");
            entity.Property(e => e.TokenHash).HasMaxLength(256).IsRequired().HasColumnName("token_hash");
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.Property(e => e.Estado).HasConversion<int>().HasColumnName("estado");
            entity.Property(e => e.AutorizadoPor).HasMaxLength(50).HasColumnName("autorizado_por");
            entity.Property(e => e.AutorizadoEm).HasColumnName("autorizado_em");
            entity.Property(e => e.UltimoAcessoEm).HasColumnName("ultimo_acesso_em");
            entity.Property(e => e.UltimoIp).HasMaxLength(50).HasColumnName("ultimo_ip");
            entity.Property(e => e.UserAgent).HasMaxLength(255).HasColumnName("user_agent");

            entity.Property(e => e.CriadoEm).HasColumnName("criado_em");
            entity.Property(e => e.CriadoPor).HasMaxLength(50).HasColumnName("criado_por");
            entity.Property(e => e.AtualizadoEm).HasColumnName("atualizado_em");
            entity.Property(e => e.AtualizadoPor).HasMaxLength(50).HasColumnName("atualizado_por");
            entity.Property(e => e.Versao).IsConcurrencyToken().HasColumnName("versao");
            entity.Property(e => e.EstaEliminado).HasDefaultValue(false).HasColumnName("esta_eliminado");
            entity.Property(e => e.EliminadoEm).HasColumnName("eliminado_em");
            entity.Property(e => e.EliminadoPor).HasMaxLength(50).HasColumnName("eliminado_por");

            entity.HasQueryFilter(e => !e.EstaEliminado);
        });

        // 7. SessaoUtilizador
        modelBuilder.Entity<SessaoUtilizador>(entity =>
        {
            entity.ToTable("sessoes_utilizador");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UtilizadorId).HasColumnName("utilizador_id");
            entity.Property(e => e.DispositivoId).HasColumnName("dispositivo_id");
            entity.Property(e => e.Inicio).HasColumnName("inicio");
            entity.Property(e => e.UltimoAcesso).HasColumnName("ultimo_acesso");
            entity.Property(e => e.Ip).HasMaxLength(50).HasColumnName("ip");
            entity.Property(e => e.UserAgent).HasMaxLength(255).HasColumnName("user_agent");
            entity.Property(e => e.Revogada).HasDefaultValue(false).HasColumnName("revogada");

            entity.HasOne(e => e.Utilizador)
                  .WithMany(u => u.Sessoes)
                  .HasForeignKey(e => e.UtilizadorId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Dispositivo)
                  .WithMany(d => d.Sessoes)
                  .HasForeignKey(e => e.DispositivoId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(e => !e.Utilizador.EstaEliminado);
        });

        // 8. TentativaLogin
        modelBuilder.Entity<TentativaLogin>(entity =>
        {
            entity.ToTable("tentativas_login");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");
            entity.Property(e => e.UtilizadorId).HasColumnName("utilizador_id");
            entity.Property(e => e.NomeTentado).HasMaxLength(50).IsRequired().HasColumnName("nome_tentado");
            entity.Property(e => e.Ip).HasMaxLength(50).HasColumnName("ip");
            entity.Property(e => e.DispositivoId).HasColumnName("dispositivo_id");
            entity.Property(e => e.Sucesso).HasColumnName("sucesso");
            entity.Property(e => e.Motivo).HasMaxLength(150).HasColumnName("motivo");
            entity.Property(e => e.DataHora).HasColumnName("data_hora");

            entity.HasOne(e => e.Utilizador)
                  .WithMany(u => u.TentativasLogin)
                  .HasForeignKey(e => e.UtilizadorId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Dispositivo)
                  .WithMany()
                  .HasForeignKey(e => e.DispositivoId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.DataHora);
            entity.HasIndex(e => new { e.NomeTentado, e.DataHora });
        });

        // 9. Restaurante
        modelBuilder.Entity<Restaurante>(entity =>
        {
            entity.ToTable("restaurante");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nome).HasMaxLength(150).IsRequired().HasColumnName("nome");
            entity.Property(e => e.Nuit).HasMaxLength(20).IsRequired().HasColumnName("nuit");
            entity.Property(e => e.Morada).HasMaxLength(255).HasColumnName("morada");
            entity.Property(e => e.Telefone).HasMaxLength(50).HasColumnName("telefone");
            entity.Property(e => e.Email).HasMaxLength(100).HasColumnName("email");
            entity.Property(e => e.RodapeTalao).HasMaxLength(255).HasColumnName("rodape_talao");
            entity.Property(e => e.Moeda).HasMaxLength(10).HasDefaultValue("MZN").HasColumnName("moeda");
            entity.Property(e => e.CasasDecimais).HasDefaultValue(2).HasColumnName("casas_decimais");
            entity.Property(e => e.FusoHorario).HasMaxLength(50).HasDefaultValue("Africa/Maputo").HasColumnName("fuso_horario");
            entity.Property(e => e.TaxaIvaPadrao).HasPrecision(5, 2).HasDefaultValue(16.00m).HasColumnName("taxa_iva_padrao");

            entity.Property(e => e.CriadoEm).HasColumnName("criado_em");
            entity.Property(e => e.CriadoPor).HasMaxLength(50).HasColumnName("criado_por");
            entity.Property(e => e.AtualizadoEm).HasColumnName("atualizado_em");
            entity.Property(e => e.AtualizadoPor).HasMaxLength(50).HasColumnName("atualizado_por");
            entity.Property(e => e.Versao).IsConcurrencyToken().HasColumnName("versao");
        });

        // 10. Configuracao
        modelBuilder.Entity<Configuracao>(entity =>
        {
            entity.ToTable("configuracoes");
            entity.HasKey(e => e.Chave);
            entity.Property(e => e.Chave).HasMaxLength(80).HasColumnName("chave");
            entity.Property(e => e.Valor).IsRequired().HasColumnName("valor");
            entity.Property(e => e.Tipo).HasMaxLength(20).HasDefaultValue("string").HasColumnName("tipo");
            entity.Property(e => e.Descricao).HasMaxLength(255).HasColumnName("descricao");
            entity.Property(e => e.AtualizadoEm).HasColumnName("atualizado_em");
        });

        // 11. Sequencia
        modelBuilder.Entity<Sequencia>(entity =>
        {
            entity.ToTable("sequencias");
            entity.HasKey(e => new { e.Serie, e.Ano });
            entity.Property(e => e.Serie).HasMaxLength(20).HasColumnName("serie");
            entity.Property(e => e.Ano).HasColumnName("ano");
            entity.Property(e => e.UltimoNumero).HasColumnName("ultimo_numero");
        });

        // 12. RegistoAuditoria (Append-only, WORM)
        modelBuilder.Entity<RegistoAuditoria>(entity =>
        {
            entity.ToTable("auditoria");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");
            entity.Property(e => e.DataHora).HasColumnName("data_hora");
            entity.Property(e => e.UtilizadorId).HasColumnName("utilizador_id");
            entity.Property(e => e.NomeUtilizadorSnapshot).HasMaxLength(50).IsRequired().HasColumnName("nome_utilizador_snapshot");
            entity.Property(e => e.Perfil).HasMaxLength(50).HasColumnName("perfil");
            entity.Property(e => e.DispositivoId).HasColumnName("dispositivo_id");
            entity.Property(e => e.Ip).HasMaxLength(50).HasColumnName("ip");
            entity.Property(e => e.Acao).HasMaxLength(80).IsRequired().HasColumnName("acao");
            entity.Property(e => e.Entidade).HasMaxLength(80).IsRequired().HasColumnName("entidade");
            entity.Property(e => e.EntidadeId).HasMaxLength(80).HasColumnName("entidade_id");
            entity.Property(e => e.CaixaId).HasColumnName("caixa_id");
            entity.Property(e => e.SessaoCaixaId).HasColumnName("sessao_caixa_id");
            entity.Property(e => e.ValoresAntes).HasColumnType("jsonb").HasColumnName("valores_antes");
            entity.Property(e => e.ValoresDepois).HasColumnType("jsonb").HasColumnName("valores_depois");
            entity.Property(e => e.Motivo).HasMaxLength(255).HasColumnName("motivo");
            entity.Property(e => e.CorrelacaoId).HasMaxLength(64).HasColumnName("correlacao_id");
            entity.Property(e => e.HashAnterior).HasMaxLength(64).IsRequired().HasColumnName("hash_anterior");
            entity.Property(e => e.Hash).HasMaxLength(64).IsRequired().HasColumnName("hash");

            entity.HasIndex(e => e.DataHora);
            entity.HasIndex(e => e.Acao);
            entity.HasIndex(e => e.UtilizadorId);
            entity.HasIndex(e => e.CorrelacaoId);
        });

        // 13. EstacaoPreparacao
        modelBuilder.Entity<EstacaoPreparacao>(entity =>
        {
            entity.ToTable("estacoes_preparacao");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired().HasColumnName("nome");
            entity.Property(e => e.TemEcra).HasDefaultValue(true).HasColumnName("tem_ecra");
            entity.Property(e => e.ImprimeTalao).HasDefaultValue(false).HasColumnName("imprime_talao");
            entity.Property(e => e.ImpressoraId).HasColumnName("impressora_id");
            entity.Property(e => e.Ativa).HasDefaultValue(true).HasColumnName("ativa");

            entity.Property(e => e.CriadoEm).HasColumnName("criado_em");
            entity.Property(e => e.CriadoPor).HasMaxLength(50).HasColumnName("criado_por");
            entity.Property(e => e.AtualizadoEm).HasColumnName("atualizado_em");
            entity.Property(e => e.AtualizadoPor).HasMaxLength(50).HasColumnName("atualizado_por");
            entity.Property(e => e.Versao).IsConcurrencyToken().HasColumnName("versao");
            entity.Property(e => e.EstaEliminado).HasDefaultValue(false).HasColumnName("esta_eliminado");
            entity.Property(e => e.EliminadoEm).HasColumnName("eliminado_em");
            entity.Property(e => e.EliminadoPor).HasMaxLength(50).HasColumnName("eliminado_por");

            entity.HasQueryFilter(e => !e.EstaEliminado);
        });

        // 14. Zona
        modelBuilder.Entity<Zona>(entity =>
        {
            entity.ToTable("zonas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired().HasColumnName("nome");
            entity.Property(e => e.Ordem).HasDefaultValue(0).HasColumnName("ordem");
            entity.Property(e => e.Ativa).HasDefaultValue(true).HasColumnName("ativa");

            entity.Property(e => e.CriadoEm).HasColumnName("criado_em");
            entity.Property(e => e.CriadoPor).HasMaxLength(50).HasColumnName("criado_por");
            entity.Property(e => e.AtualizadoEm).HasColumnName("atualizado_em");
            entity.Property(e => e.AtualizadoPor).HasMaxLength(50).HasColumnName("atualizado_por");
            entity.Property(e => e.Versao).IsConcurrencyToken().HasColumnName("versao");
            entity.Property(e => e.EstaEliminado).HasDefaultValue(false).HasColumnName("esta_eliminado");
            entity.Property(e => e.EliminadoEm).HasColumnName("eliminado_em");
            entity.Property(e => e.EliminadoPor).HasMaxLength(50).HasColumnName("eliminado_por");

            entity.HasQueryFilter(e => !e.EstaEliminado);
        });

        // 15. Mesa
        modelBuilder.Entity<Mesa>(entity =>
        {
            entity.ToTable("mesas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ZonaId).HasColumnName("zona_id");
            entity.Property(e => e.Numero).HasMaxLength(20).IsRequired().HasColumnName("numero");
            entity.Property(e => e.Lugares).HasDefaultValue(4).HasColumnName("lugares");
            entity.Property(e => e.PosicaoX).HasDefaultValue(0).HasColumnName("posicao_x");
            entity.Property(e => e.PosicaoY).HasDefaultValue(0).HasColumnName("posicao_y");
            entity.Property(e => e.Estado).HasConversion<int>().HasColumnName("estado");
            entity.Property(e => e.Ativa).HasDefaultValue(true).HasColumnName("ativa");

            entity.HasOne(e => e.Zona)
                  .WithMany(z => z.Mesas)
                  .HasForeignKey(e => e.ZonaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.CriadoEm).HasColumnName("criado_em");
            entity.Property(e => e.CriadoPor).HasMaxLength(50).HasColumnName("criado_por");
            entity.Property(e => e.AtualizadoEm).HasColumnName("atualizado_em");
            entity.Property(e => e.AtualizadoPor).HasMaxLength(50).HasColumnName("atualizado_por");
            entity.Property(e => e.Versao).IsConcurrencyToken().HasColumnName("versao");
            entity.Property(e => e.EstaEliminado).HasDefaultValue(false).HasColumnName("esta_eliminado");
            entity.Property(e => e.EliminadoEm).HasColumnName("eliminado_em");
            entity.Property(e => e.EliminadoPor).HasMaxLength(50).HasColumnName("eliminado_por");

            entity.HasIndex(e => new { e.ZonaId, e.Numero }).IsUnique();
            entity.HasQueryFilter(e => !e.EstaEliminado && !e.Zona.EstaEliminado);
        });

        // 16. ContaMesa
        modelBuilder.Entity<ContaMesa>(entity =>
        {
            entity.ToTable("contas_mesas");
            entity.HasKey(e => new { e.ContaId, e.MesaId });
            entity.Property(e => e.ContaId).HasColumnName("conta_id");
            entity.Property(e => e.MesaId).HasColumnName("mesa_id");
            entity.Property(e => e.AdicionadaEm).HasColumnName("adicionada_em");

            entity.HasOne(e => e.Mesa)
                  .WithMany(m => m.ContasMesas)
                  .HasForeignKey(e => e.MesaId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.Mesa.EstaEliminado);
        });

        // 17. Categoria
        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.ToTable("categorias");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired().HasColumnName("nome");
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.Icone).HasMaxLength(50).HasColumnName("icone");
            entity.Property(e => e.Ordem).HasDefaultValue(0).HasColumnName("ordem");
            entity.Property(e => e.EstacaoPadraoId).HasColumnName("estacao_padrao_id");
            entity.Property(e => e.Ativa).HasDefaultValue(true).HasColumnName("ativa");

            entity.HasOne(e => e.Parent)
                  .WithMany(c => c.Subcategorias)
                  .HasForeignKey(e => e.ParentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.EstacaoPadrao)
                  .WithMany()
                  .HasForeignKey(e => e.EstacaoPadraoId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.CriadoEm).HasColumnName("criado_em");
            entity.Property(e => e.CriadoPor).HasMaxLength(50).HasColumnName("criado_por");
            entity.Property(e => e.AtualizadoEm).HasColumnName("atualizado_em");
            entity.Property(e => e.AtualizadoPor).HasMaxLength(50).HasColumnName("atualizado_por");
            entity.Property(e => e.Versao).IsConcurrencyToken().HasColumnName("versao");
            entity.Property(e => e.EstaEliminado).HasDefaultValue(false).HasColumnName("esta_eliminado");
            entity.Property(e => e.EliminadoEm).HasColumnName("eliminado_em");
            entity.Property(e => e.EliminadoPor).HasMaxLength(50).HasColumnName("eliminado_por");

            entity.HasQueryFilter(e => !e.EstaEliminado);
        });

        // 18. Artigo
        modelBuilder.Entity<Artigo>(entity =>
        {
            entity.ToTable("artigos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CategoriaId).HasColumnName("categoria_id");
            entity.Property(e => e.CodigoInterno).HasMaxLength(50).IsRequired().HasColumnName("codigo_interno");
            entity.HasIndex(e => e.CodigoInterno).IsUnique();
            entity.Property(e => e.Nome).HasMaxLength(150).IsRequired().HasColumnName("nome");
            entity.Property(e => e.DescricaoCurta).HasMaxLength(255).HasColumnName("descricao_curta");
            entity.Property(e => e.DescricaoLonga).HasColumnName("descricao_longa");
            entity.Property(e => e.Preco).HasPrecision(18, 4).HasColumnName("preco");
            entity.Property(e => e.TaxaIva).HasPrecision(5, 2).HasDefaultValue(16.00m).HasColumnName("taxa_iva");
            entity.Property(e => e.EstacaoId).HasColumnName("estacao_id");
            entity.Property(e => e.TempoPreparacaoMin).HasDefaultValue(15).HasColumnName("tempo_preparacao_min");
            entity.Property(e => e.Destaque).HasDefaultValue(false).HasColumnName("destaque");
            entity.Property(e => e.EsgotadoManual).HasDefaultValue(false).HasColumnName("esgotado_manual");
            entity.Property(e => e.Disponivel).HasDefaultValue(true).HasColumnName("disponivel");
            entity.Property(e => e.Ordem).HasDefaultValue(0).HasColumnName("ordem");
            entity.Property(e => e.Ativo).HasDefaultValue(true).HasColumnName("ativo");

            entity.HasOne(e => e.Categoria)
                  .WithMany(c => c.Artigos)
                  .HasForeignKey(e => e.CategoriaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Estacao)
                  .WithMany()
                  .HasForeignKey(e => e.EstacaoId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.CriadoEm).HasColumnName("criado_em");
            entity.Property(e => e.CriadoPor).HasMaxLength(50).HasColumnName("criado_por");
            entity.Property(e => e.AtualizadoEm).HasColumnName("atualizado_em");
            entity.Property(e => e.AtualizadoPor).HasMaxLength(50).HasColumnName("atualizado_por");
            entity.Property(e => e.Versao).IsConcurrencyToken().HasColumnName("versao");
            entity.Property(e => e.EstaEliminado).HasDefaultValue(false).HasColumnName("esta_eliminado");
            entity.Property(e => e.EliminadoEm).HasColumnName("eliminado_em");
            entity.Property(e => e.EliminadoPor).HasMaxLength(50).HasColumnName("eliminado_por");

            entity.HasIndex(e => e.Nome);
            entity.HasIndex(e => e.EsgotadoManual);
            entity.HasQueryFilter(e => !e.EstaEliminado && !e.Categoria.EstaEliminado);
        });

        // 19. ArtigoCodigoBarras
        modelBuilder.Entity<ArtigoCodigoBarras>(entity =>
        {
            entity.ToTable("artigos_codigos_barras");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ArtigoId).HasColumnName("artigo_id");
            entity.Property(e => e.Codigo).HasMaxLength(50).IsRequired().HasColumnName("codigo");
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.Property(e => e.Tipo).HasMaxLength(20).HasDefaultValue("EAN13").HasColumnName("tipo");

            entity.HasOne(e => e.Artigo)
                  .WithMany(a => a.CodigosBarras)
                  .HasForeignKey(e => e.ArtigoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.Artigo.EstaEliminado);
        });

        // 20. ArtigoImagem
        modelBuilder.Entity<ArtigoImagem>(entity =>
        {
            entity.ToTable("artigos_imagens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ArtigoId).HasColumnName("artigo_id");
            entity.Property(e => e.CaminhoBase).HasMaxLength(255).IsRequired().HasColumnName("caminho_base");
            entity.Property(e => e.Ordem).HasDefaultValue(0).HasColumnName("ordem");
            entity.Property(e => e.Principal).HasDefaultValue(false).HasColumnName("principal");
            entity.Property(e => e.Largura).HasColumnName("largura");
            entity.Property(e => e.Altura).HasColumnName("altura");
            entity.Property(e => e.Hash).HasMaxLength(64).HasColumnName("hash");

            entity.HasOne(e => e.Artigo)
                  .WithMany(a => a.Imagens)
                  .HasForeignKey(e => e.ArtigoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.Artigo.EstaEliminado);
        });

        // 21. Alergenio
        modelBuilder.Entity<Alergenio>(entity =>
        {
            entity.ToTable("alergenios");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Codigo).HasMaxLength(50).IsRequired().HasColumnName("codigo");
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired().HasColumnName("nome");
            entity.Property(e => e.Icone).HasMaxLength(50).HasColumnName("icone");
        });

        // 22. ArtigoAlergenio
        modelBuilder.Entity<ArtigoAlergenio>(entity =>
        {
            entity.ToTable("artigos_alergenios");
            entity.HasKey(e => new { e.ArtigoId, e.AlergenioId });
            entity.Property(e => e.ArtigoId).HasColumnName("artigo_id");
            entity.Property(e => e.AlergenioId).HasColumnName("alergenio_id");

            entity.HasOne(e => e.Artigo)
                  .WithMany(a => a.ArtigosAlergenios)
                  .HasForeignKey(e => e.ArtigoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Alergenio)
                  .WithMany(a => a.ArtigosAlergenios)
                  .HasForeignKey(e => e.AlergenioId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.Artigo.EstaEliminado);
        });

        // 23. Etiqueta
        modelBuilder.Entity<Etiqueta>(entity =>
        {
            entity.ToTable("etiquetas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Codigo).HasMaxLength(50).IsRequired().HasColumnName("codigo");
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired().HasColumnName("nome");
            entity.Property(e => e.CorHex).HasMaxLength(20).HasDefaultValue("#6E1A33").HasColumnName("cor_hex");
        });

        // 24. ArtigoEtiqueta
        modelBuilder.Entity<ArtigoEtiqueta>(entity =>
        {
            entity.ToTable("artigos_etiquetas");
            entity.HasKey(e => new { e.ArtigoId, e.EtiquetaId });
            entity.Property(e => e.ArtigoId).HasColumnName("artigo_id");
            entity.Property(e => e.EtiquetaId).HasColumnName("etiqueta_id");

            entity.HasOne(e => e.Artigo)
                  .WithMany(a => a.ArtigosEtiquetas)
                  .HasForeignKey(e => e.ArtigoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Etiqueta)
                  .WithMany(t => t.ArtigosEtiquetas)
                  .HasForeignKey(e => e.EtiquetaId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.Artigo.EstaEliminado);
        });

        // 25. GrupoOpcoes
        modelBuilder.Entity<GrupoOpcoes>(entity =>
        {
            entity.ToTable("grupos_opcoes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired().HasColumnName("nome");
            entity.Property(e => e.Obrigatorio).HasDefaultValue(false).HasColumnName("obrigatorio");
            entity.Property(e => e.Min).HasDefaultValue(0).HasColumnName("min");
            entity.Property(e => e.Max).HasDefaultValue(1).HasColumnName("max");

            entity.Property(e => e.CriadoEm).HasColumnName("criado_em");
            entity.Property(e => e.CriadoPor).HasMaxLength(50).HasColumnName("criado_por");
            entity.Property(e => e.AtualizadoEm).HasColumnName("atualizado_em");
            entity.Property(e => e.AtualizadoPor).HasMaxLength(50).HasColumnName("atualizado_por");
            entity.Property(e => e.Versao).IsConcurrencyToken().HasColumnName("versao");
            entity.Property(e => e.EstaEliminado).HasDefaultValue(false).HasColumnName("esta_eliminado");
            entity.Property(e => e.EliminadoEm).HasColumnName("eliminado_em");
            entity.Property(e => e.EliminadoPor).HasMaxLength(50).HasColumnName("eliminado_por");

            entity.HasQueryFilter(e => !e.EstaEliminado);
        });

        // 26. Opcao
        modelBuilder.Entity<Opcao>(entity =>
        {
            entity.ToTable("opcoes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GrupoId).HasColumnName("grupo_id");
            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired().HasColumnName("nome");
            entity.Property(e => e.DeltaPreco).HasPrecision(18, 4).HasDefaultValue(0.00m).HasColumnName("delta_preco");
            entity.Property(e => e.Ordem).HasDefaultValue(0).HasColumnName("ordem");

            entity.HasOne(e => e.Grupo)
                  .WithMany(g => g.Opcoes)
                  .HasForeignKey(e => e.GrupoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.Grupo.EstaEliminado);
        });

        // 27. ArtigoGrupoOpcoes
        modelBuilder.Entity<ArtigoGrupoOpcoes>(entity =>
        {
            entity.ToTable("artigos_grupos_opcoes");
            entity.HasKey(e => new { e.ArtigoId, e.GrupoId });
            entity.Property(e => e.ArtigoId).HasColumnName("artigo_id");
            entity.Property(e => e.GrupoId).HasColumnName("grupo_id");

            entity.HasOne(e => e.Artigo)
                  .WithMany(a => a.ArtigosGruposOpcoes)
                  .HasForeignKey(e => e.ArtigoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Grupo)
                  .WithMany(g => g.ArtigosGruposOpcoes)
                  .HasForeignKey(e => e.GrupoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.Artigo.EstaEliminado && !e.Grupo.EstaEliminado);
        });

        // 28. MenuVersao
        modelBuilder.Entity<MenuVersao>(entity =>
        {
            entity.ToTable("menu_versao");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever().HasColumnName("id");
            entity.Property(e => e.Versao).HasColumnName("versao");
            entity.Property(e => e.AtualizadoEm).HasColumnName("atualizado_em");
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<AuditableEntity>();
        var agora = DateTime.UtcNow;
        var utilizador = _currentUserService?.NomeUtilizador ?? "sistema";

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CriadoEm = agora;
                entry.Entity.CriadoPor = utilizador;
                entry.Entity.Versao = 1;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.AtualizadoEm = agora;
                entry.Entity.AtualizadoPor = utilizador;
                entry.Entity.Versao++;
            }
        }

        var softDeleteEntries = ChangeTracker.Entries<SoftDeletableEntity>();
        foreach (var entry in softDeleteEntries)
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.EstaEliminado = true;
                entry.Entity.EliminadoEm = agora;
                entry.Entity.EliminadoPor = utilizador;
                entry.Entity.Versao++;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
