using Menova.Application.DTOs;
using Menova.Application.Interfaces;
using Menova.Domain.Menu;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Menova.Infrastructure.Services;

public class MenuService : IMenuService
{
    private const string MenuCacheKey = "menova_menu_completo";
    private readonly IAppDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly IAuditoriaService _auditoria;
    private readonly INotificacaoService _notificacao;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<MenuService> _logger;

    public MenuService(
        IAppDbContext context,
        IMemoryCache cache,
        IAuditoriaService auditoria,
        INotificacaoService notificacao,
        ICurrentUserService currentUser,
        ILogger<MenuService> logger)
    {
        _context = context;
        _cache = cache;
        _auditoria = auditoria;
        _notificacao = notificacao;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<MenuCompletoDto> GetMenuCompletoAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(MenuCacheKey, out MenuCompletoDto? cached) && cached != null)
        {
            return cached;
        }

        var versaoEntidade = await _context.MenuVersao.AsNoTracking().FirstOrDefaultAsync(v => v.Id == 1, cancellationToken);
        var versao = versaoEntidade?.Versao ?? 1;
        var atualizadoEm = versaoEntidade?.AtualizadoEm ?? DateTime.UtcNow;

        var categorias = await _context.Categorias
            .AsNoTracking()
            .Where(c => c.Ativa)
            .OrderBy(c => c.Ordem)
            .Select(c => new CategoriaDto(
                c.Id,
                c.Nome,
                c.ParentId,
                c.Icone,
                c.Ordem,
                c.EstacaoPadraoId,
                c.EstacaoPadrao != null ? c.EstacaoPadrao.Nome : null,
                c.Artigos.Count(a => a.Ativo),
                c.Ativa))
            .ToListAsync(cancellationToken);

        var artigos = await _context.Artigos
            .AsNoTracking()
            .Include(a => a.Categoria)
            .Include(a => a.Estacao)
            .Include(a => a.CodigosBarras)
            .Include(a => a.Imagens)
            .Include(a => a.ArtigosAlergenios).ThenInclude(aa => aa.Alergenio)
            .Include(a => a.ArtigosEtiquetas).ThenInclude(ae => ae.Etiqueta)
            .Where(a => a.Ativo)
            .OrderBy(a => a.Ordem)
            .Select(a => new ArtigoDto(
                a.Id,
                a.CategoriaId,
                a.Categoria.Nome,
                a.CodigoInterno,
                a.Nome,
                a.DescricaoCurta,
                a.Preco,
                a.TaxaIva,
                a.EstacaoId,
                a.Estacao != null ? a.Estacao.Nome : null,
                a.TempoPreparacaoMin,
                a.Destaque,
                a.EsgotadoManual,
                a.Disponivel,
                a.Ativo,
                a.Imagens.Where(i => i.Principal).Select(i => i.CaminhoBase).FirstOrDefault() ?? "/img/artigo-placeholder.jpg",
                a.ArtigosAlergenios.Select(al => al.Alergenio.Nome).ToList(),
                a.ArtigosEtiquetas.Select(et => et.Etiqueta.Nome).ToList()))
            .ToListAsync(cancellationToken);

        var dto = new MenuCompletoDto(versao, atualizadoEm, categorias, artigos);
        _cache.Set(MenuCacheKey, dto, TimeSpan.FromHours(4));
        return dto;
    }

    public async Task<long> GetMenuVersaoAsync(CancellationToken cancellationToken = default)
    {
        var v = await _context.MenuVersao.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1, cancellationToken);
        return v?.Versao ?? 1;
    }

    public async Task<List<CategoriaDto>> GetCategoriasAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categorias
            .AsNoTracking()
            .OrderBy(c => c.Ordem)
            .Select(c => new CategoriaDto(
                c.Id,
                c.Nome,
                c.ParentId,
                c.Icone,
                c.Ordem,
                c.EstacaoPadraoId,
                c.EstacaoPadrao != null ? c.EstacaoPadrao.Nome : null,
                c.Artigos.Count(a => a.Ativo),
                c.Ativa))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ArtigoDto>> GetArtigosPorCategoriaAsync(Guid? categoriaId, string? termoPesquisa = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Artigos
            .AsNoTracking()
            .Include(a => a.Categoria)
            .Include(a => a.Estacao)
            .Include(a => a.Imagens)
            .Include(a => a.ArtigosAlergenios).ThenInclude(aa => aa.Alergenio)
            .Include(a => a.ArtigosEtiquetas).ThenInclude(ae => ae.Etiqueta)
            .Where(a => a.Ativo);

        if (categoriaId.HasValue && categoriaId.Value != Guid.Empty)
        {
            query = query.Where(a => a.CategoriaId == categoriaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(termoPesquisa))
        {
            var termo = termoPesquisa.Trim().ToLower();
            query = query.Where(a => a.Nome.ToLower().Contains(termo) ||
                                     a.CodigoInterno.ToLower().Contains(termo) ||
                                     a.CodigosBarras.Any(cb => cb.Codigo.Contains(termo)));
        }

        return await query
            .OrderBy(a => a.Ordem)
            .ThenBy(a => a.Nome)
            .Select(a => new ArtigoDto(
                a.Id,
                a.CategoriaId,
                a.Categoria.Nome,
                a.CodigoInterno,
                a.Nome,
                a.DescricaoCurta,
                a.Preco,
                a.TaxaIva,
                a.EstacaoId,
                a.Estacao != null ? a.Estacao.Nome : null,
                a.TempoPreparacaoMin,
                a.Destaque,
                a.EsgotadoManual,
                a.Disponivel,
                a.Ativo,
                a.Imagens.Where(i => i.Principal).Select(i => i.CaminhoBase).FirstOrDefault() ?? "/img/artigo-placeholder.jpg",
                a.ArtigosAlergenios.Select(al => al.Alergenio.Nome).ToList(),
                a.ArtigosEtiquetas.Select(et => et.Etiqueta.Nome).ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<ArtigoDetalheDto?> GetArtigoDetalheAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var artigo = await _context.Artigos
            .AsNoTracking()
            .Include(a => a.Categoria)
            .Include(a => a.Estacao)
            .Include(a => a.Imagens)
            .Include(a => a.ArtigosAlergenios).ThenInclude(aa => aa.Alergenio)
            .Include(a => a.ArtigosEtiquetas).ThenInclude(ae => ae.Etiqueta)
            .Include(a => a.ArtigosGruposOpcoes).ThenInclude(ago => ago.Grupo).ThenInclude(g => g.Opcoes)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (artigo == null) return null;

        return new ArtigoDetalheDto(
            artigo.Id,
            artigo.CategoriaId,
            artigo.Categoria.Nome,
            artigo.CodigoInterno,
            artigo.Nome,
            artigo.DescricaoCurta,
            artigo.DescricaoLonga,
            artigo.Preco,
            artigo.TaxaIva,
            artigo.EstacaoId,
            artigo.Estacao?.Nome,
            artigo.TempoPreparacaoMin,
            artigo.Destaque,
            artigo.EsgotadoManual,
            artigo.Disponivel,
            artigo.Ativo,
            artigo.Imagens.OrderBy(i => i.Ordem).Select(i => i.CaminhoBase).ToList(),
            artigo.ArtigosAlergenios.Select(aa => new AlergenioDto(aa.Alergenio.Id, aa.Alergenio.Codigo, aa.Alergenio.Nome, aa.Alergenio.Icone)).ToList(),
            artigo.ArtigosEtiquetas.Select(ae => new EtiquetaDto(ae.Etiqueta.Id, ae.Etiqueta.Codigo, ae.Etiqueta.Nome, ae.Etiqueta.CorHex)).ToList(),
            artigo.ArtigosGruposOpcoes.Select(ago => new GrupoOpcoesDto(
                ago.Grupo.Id,
                ago.Grupo.Nome,
                ago.Grupo.Obrigatorio,
                ago.Grupo.Min,
                ago.Grupo.Max,
                ago.Grupo.Opcoes.OrderBy(o => o.Ordem).Select(o => new OpcaoDto(o.Id, o.Nome, o.DeltaPreco, o.Ordem)).ToList()
            )).ToList()
        );
    }

    public async Task<Toggle86Response> ToggleEsgotadoManualAsync(Guid artigoId, string? motivo = null, CancellationToken cancellationToken = default)
    {
        var artigo = await _context.Artigos.FirstOrDefaultAsync(a => a.Id == artigoId, cancellationToken)
            ?? throw new KeyNotFoundException($"Artigo com ID {artigoId} não foi encontrado.");

        var estadoAnterior = artigo.EsgotadoManual;
        artigo.EsgotadoManual = !estadoAnterior;

        var versao = await IncrementarVersaoMenuAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditoria.RegistarAsync(
            acao: "ARTIGO.TOGGLE_86",
            entidade: "artigos",
            entidadeId: artigo.Id.ToString(),
            valoresAntes: new { EsgotadoManual = estadoAnterior },
            valoresDepois: new { EsgotadoManual = artigo.EsgotadoManual },
            motivo: motivo ?? (artigo.EsgotadoManual ? "Artigo marcado como esgotado (86)" : "Artigo reaberto para venda"),
            cancellationToken: cancellationToken);

        InvalidarCacheInterna();

        await _notificacao.NotificarArtigoEsgotadoAlteradoAsync(
            artigo.Id,
            artigo.Nome,
            artigo.EsgotadoManual,
            versao,
            cancellationToken);

        _logger.LogInformation("Artigo {Nome} ({Id}) alterado esgotado (86) para {Estado}", artigo.Nome, artigo.Id, artigo.EsgotadoManual);

        return new Toggle86Response(artigo.Id, artigo.Nome, artigo.EsgotadoManual, versao, DateTime.UtcNow);
    }

    public async Task<Guid> CriarArtigoAsync(CriarArtigoDto dto, CancellationToken cancellationToken = default)
    {
        var artigo = new Artigo
        {
            CategoriaId = dto.CategoriaId,
            CodigoInterno = dto.CodigoInterno.Trim().ToUpperInvariant(),
            Nome = dto.Nome.Trim(),
            DescricaoCurta = dto.DescricaoCurta,
            DescricaoLonga = dto.DescricaoLonga,
            Preco = dto.Preco,
            TaxaIva = dto.TaxaIva,
            EstacaoId = dto.EstacaoId,
            TempoPreparacaoMin = dto.TempoPreparacaoMin,
            Destaque = dto.Destaque,
            Ativo = true,
            Disponivel = true,
            EsgotadoManual = false
        };

        _context.Artigos.Add(artigo);
        await IncrementarVersaoMenuAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditoria.RegistarAsync(
            acao: "ARTIGO.CRIAR",
            entidade: "artigos",
            entidadeId: artigo.Id.ToString(),
            valoresDepois: artigo,
            motivo: "Criação de novo artigo no menu",
            cancellationToken: cancellationToken);

        InvalidarCacheInterna();
        return artigo.Id;
    }

    public async Task AtualizarArtigoAsync(AtualizarArtigoDto dto, CancellationToken cancellationToken = default)
    {
        var artigo = await _context.Artigos.FirstOrDefaultAsync(a => a.Id == dto.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Artigo com ID {dto.Id} não foi encontrado.");

        var antes = new { artigo.Nome, artigo.Preco, artigo.TaxaIva, artigo.Ativo };

        artigo.CategoriaId = dto.CategoriaId;
        artigo.CodigoInterno = dto.CodigoInterno.Trim().ToUpperInvariant();
        artigo.Nome = dto.Nome.Trim();
        artigo.DescricaoCurta = dto.DescricaoCurta;
        artigo.DescricaoLonga = dto.DescricaoLonga;
        artigo.Preco = dto.Preco;
        artigo.TaxaIva = dto.TaxaIva;
        artigo.EstacaoId = dto.EstacaoId;
        artigo.TempoPreparacaoMin = dto.TempoPreparacaoMin;
        artigo.Destaque = dto.Destaque;
        artigo.Ativo = dto.Ativo;

        await IncrementarVersaoMenuAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditoria.RegistarAsync(
            acao: "ARTIGO.ATUALIZAR",
            entidade: "artigos",
            entidadeId: artigo.Id.ToString(),
            valoresAntes: antes,
            valoresDepois: new { artigo.Nome, artigo.Preco, artigo.TaxaIva, artigo.Ativo },
            motivo: "Atualização de dados do artigo",
            cancellationToken: cancellationToken);

        InvalidarCacheInterna();
    }

    public async Task EliminarArtigoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var artigo = await _context.Artigos.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Artigo com ID {id} não foi encontrado.");

        _context.Artigos.Remove(artigo); // Soft delete automático do SaveChangesAsync
        await IncrementarVersaoMenuAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditoria.RegistarAsync(
            acao: "ARTIGO.ELIMINAR",
            entidade: "artigos",
            entidadeId: id.ToString(),
            valoresAntes: new { artigo.Nome, artigo.CodigoInterno },
            motivo: "Remoção do artigo (soft delete)",
            cancellationToken: cancellationToken);

        InvalidarCacheInterna();
    }

    public async Task<Guid> CriarCategoriaAsync(CriarCategoriaDto dto, CancellationToken cancellationToken = default)
    {
        var categoria = new Categoria
        {
            Nome = dto.Nome.Trim(),
            ParentId = dto.ParentId,
            Icone = dto.Icone ?? "bi-tag",
            Ordem = dto.Ordem,
            EstacaoPadraoId = dto.EstacaoPadraoId,
            Ativa = true
        };

        _context.Categorias.Add(categoria);
        await IncrementarVersaoMenuAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditoria.RegistarAsync(
            acao: "CATEGORIA.CRIAR",
            entidade: "categorias",
            entidadeId: categoria.Id.ToString(),
            valoresDepois: categoria,
            cancellationToken: cancellationToken);

        InvalidarCacheInterna();
        return categoria.Id;
    }

    public async Task AtualizarCategoriaAsync(AtualizarCategoriaDto dto, CancellationToken cancellationToken = default)
    {
        var categoria = await _context.Categorias.FirstOrDefaultAsync(c => c.Id == dto.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Categoria com ID {dto.Id} não foi encontrada.");

        categoria.Nome = dto.Nome.Trim();
        categoria.ParentId = dto.ParentId;
        categoria.Icone = dto.Icone;
        categoria.Ordem = dto.Ordem;
        categoria.EstacaoPadraoId = dto.EstacaoPadraoId;
        categoria.Ativa = dto.Ativa;

        await IncrementarVersaoMenuAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditoria.RegistarAsync(
            acao: "CATEGORIA.ATUALIZAR",
            entidade: "categorias",
            entidadeId: categoria.Id.ToString(),
            valoresDepois: categoria,
            cancellationToken: cancellationToken);

        InvalidarCacheInterna();
    }

    public Task InvalidarCacheAsync(CancellationToken cancellationToken = default)
    {
        InvalidarCacheInterna();
        return Task.CompletedTask;
    }

    private void InvalidarCacheInterna()
    {
        _cache.Remove(MenuCacheKey);
    }

    private async Task<long> IncrementarVersaoMenuAsync(CancellationToken cancellationToken)
    {
        var versaoObj = await _context.MenuVersao.FirstOrDefaultAsync(v => v.Id == 1, cancellationToken);
        if (versaoObj == null)
        {
            versaoObj = new MenuVersao { Id = 1, Versao = 1, AtualizadoEm = DateTime.UtcNow };
            _context.MenuVersao.Add(versaoObj);
        }
        else
        {
            versaoObj.Versao++;
            versaoObj.AtualizadoEm = DateTime.UtcNow;
        }
        return versaoObj.Versao;
    }
}
