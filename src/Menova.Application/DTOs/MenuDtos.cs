namespace Menova.Application.DTOs;

public record CategoriaDto(
    Guid Id,
    string Nome,
    Guid? ParentId,
    string? Icone,
    int Ordem,
    Guid? EstacaoPadraoId,
    string? EstacaoPadraoNome,
    int TotalArtigos,
    bool Ativa);

public record ArtigoDto(
    Guid Id,
    Guid CategoriaId,
    string CategoriaNome,
    string CodigoInterno,
    string Nome,
    string? DescricaoCurta,
    decimal Preco,
    decimal TaxaIva,
    Guid? EstacaoId,
    string? EstacaoNome,
    int TempoPreparacaoMin,
    bool Destaque,
    bool EsgotadoManual,
    bool Disponivel,
    bool Ativo,
    string? ImagemPrincipal,
    List<string> Alergenios,
    List<string> Etiquetas);

public record ArtigoDetalheDto(
    Guid Id,
    Guid CategoriaId,
    string CategoriaNome,
    string CodigoInterno,
    string Nome,
    string? DescricaoCurta,
    string? DescricaoLonga,
    decimal Preco,
    decimal TaxaIva,
    Guid? EstacaoId,
    string? EstacaoNome,
    int TempoPreparacaoMin,
    bool Destaque,
    bool EsgotadoManual,
    bool Disponivel,
    bool Ativo,
    List<string> Imagens,
    List<AlergenioDto> Alergenios,
    List<EtiquetaDto> Etiquetas,
    List<GrupoOpcoesDto> GruposOpcoes);

public record AlergenioDto(Guid Id, string Codigo, string Nome, string? Icone);

public record EtiquetaDto(Guid Id, string Codigo, string Nome, string CorHex);

public record OpcaoDto(Guid Id, string Nome, decimal DeltaPreco, int Ordem);

public record GrupoOpcoesDto(Guid Id, string Nome, bool Obrigatorio, int Min, int Max, List<OpcaoDto> Opcoes);

public record CriarArtigoDto(
    Guid CategoriaId,
    string CodigoInterno,
    string Nome,
    string? DescricaoCurta,
    string? DescricaoLonga,
    decimal Preco,
    decimal TaxaIva,
    Guid? EstacaoId,
    int TempoPreparacaoMin,
    bool Destaque);

public record AtualizarArtigoDto(
    Guid Id,
    Guid CategoriaId,
    string CodigoInterno,
    string Nome,
    string? DescricaoCurta,
    string? DescricaoLonga,
    decimal Preco,
    decimal TaxaIva,
    Guid? EstacaoId,
    int TempoPreparacaoMin,
    bool Destaque,
    bool Ativo);

public record CriarCategoriaDto(
    string Nome,
    Guid? ParentId,
    string? Icone,
    int Ordem,
    Guid? EstacaoPadraoId);

public record AtualizarCategoriaDto(
    Guid Id,
    string Nome,
    Guid? ParentId,
    string? Icone,
    int Ordem,
    Guid? EstacaoPadraoId,
    bool Ativa);

public record Toggle86Request(string? Motivo);

public record Toggle86Response(
    Guid ArtigoId,
    string Nome,
    bool EsgotadoManual,
    long MenuVersao,
    DateTime DataHoraUtc);

public record MenuCompletoDto(
    long Versao,
    DateTime AtualizadoEm,
    List<CategoriaDto> Categorias,
    List<ArtigoDto> Artigos);
