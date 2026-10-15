using Menova.Application.DTOs;

namespace Menova.Application.Interfaces;

public interface IMenuService
{
    Task<MenuCompletoDto> GetMenuCompletoAsync(CancellationToken cancellationToken = default);
    Task<long> GetMenuVersaoAsync(CancellationToken cancellationToken = default);
    Task<List<CategoriaDto>> GetCategoriasAsync(CancellationToken cancellationToken = default);
    Task<List<ArtigoDto>> GetArtigosPorCategoriaAsync(Guid? categoriaId, string? termoPesquisa = null, CancellationToken cancellationToken = default);
    Task<ArtigoDetalheDto?> GetArtigoDetalheAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Toggle86Response> ToggleEsgotadoManualAsync(Guid artigoId, string? motivo = null, CancellationToken cancellationToken = default);
    Task<Guid> CriarArtigoAsync(CriarArtigoDto dto, CancellationToken cancellationToken = default);
    Task AtualizarArtigoAsync(AtualizarArtigoDto dto, CancellationToken cancellationToken = default);
    Task EliminarArtigoAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CriarCategoriaAsync(CriarCategoriaDto dto, CancellationToken cancellationToken = default);
    Task AtualizarCategoriaAsync(AtualizarCategoriaDto dto, CancellationToken cancellationToken = default);
    Task InvalidarCacheAsync(CancellationToken cancellationToken = default);
}
