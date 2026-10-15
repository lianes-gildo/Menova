using Menova.Domain.Impressao;

namespace Menova.Application.DTOs;

public record ImpressoraDto(
    Guid Id,
    string Nome,
    string EnderecoIp,
    int Porta,
    int LarguraColunas,
    bool Ativa,
    bool CortarPapel,
    bool AbrirGaveta);

public record CriarImpressoraDto(
    string Nome,
    string EnderecoIp,
    int Porta,
    int LarguraColunas,
    bool CortarPapel,
    bool AbrirGaveta);

public record EnviarImpressaoDto(
    Guid? ImpressoraId,
    TipoDocumentoImpressao Tipo,
    byte[] ConteudoBytes);
