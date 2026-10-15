using Menova.Application.DTOs;
using Menova.Domain.Impressao;

namespace Menova.Application.Interfaces;

public interface IImpressaoService
{
    Task<List<ImpressoraDto>> ObterImpressorasAsync(CancellationToken ct = default);
    Task<ImpressoraDto?> ObterImpressoraPrincipalAsync(CancellationToken ct = default);
    Task<bool> EnviarParaImpressoraAsync(Guid? impressoraId, byte[] dados, TipoDocumentoImpressao tipo, CancellationToken ct = default);
    byte[] GerarTalaoConta(ContaDto conta, bool eSegundaVia = false);
    byte[] GerarReciboPagamento(ContaDto conta, PagamentoDto pagamento, decimal saldoRestante, bool eSegundaVia = false);
    byte[] GerarTalaoCozinha(TicketKdsDto ticket);
    byte[] GerarRelatorioX(RelatorioXDto relatorio);
    byte[] GerarTalaoZ(DocumentoZDto docZ, bool eSegundaVia = false);
    byte[] GerarComprovativoMovimento(MovimentoCaixaDto movimento, string caixaNome);
}
