using System.Net.Sockets;
using Menova.Application.DTOs;
using Menova.Application.Interfaces;
using Menova.Domain.Impressao;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Menova.Infrastructure.Services;

public class ImpressaoService : IImpressaoService
{
    private readonly IAppDbContext _context;
    private readonly ILogger<ImpressaoService> _logger;
    private readonly EscPosGenerator _generator;

    public ImpressaoService(IAppDbContext context, ILogger<ImpressaoService> logger)
    {
        _context = context;
        _logger = logger;
        _generator = new EscPosGenerator(larguraColunas: 32);
    }

    public async Task<List<ImpressoraDto>> ObterImpressorasAsync(CancellationToken ct = default)
    {
        return await _context.Impressoras
            .AsNoTracking()
            .Where(i => i.Ativa)
            .OrderBy(i => i.Nome)
            .Select(i => new ImpressoraDto(
                i.Id,
                i.Nome,
                i.EnderecoIp,
                i.Porta,
                i.LarguraColunas,
                i.Ativa,
                i.CortarPapel,
                i.AbrirGaveta
            ))
            .ToListAsync(ct);
    }

    public async Task<ImpressoraDto?> ObterImpressoraPrincipalAsync(CancellationToken ct = default)
    {
        var imp = await _context.Impressoras
            .AsNoTracking()
            .Where(i => i.Ativa)
            .OrderBy(i => i.Nome)
            .FirstOrDefaultAsync(ct);

        if (imp == null) return null;

        return new ImpressoraDto(
            imp.Id,
            imp.Nome,
            imp.EnderecoIp,
            imp.Porta,
            imp.LarguraColunas,
            imp.Ativa,
            imp.CortarPapel,
            imp.AbrirGaveta
        );
    }

    public async Task<bool> EnviarParaImpressoraAsync(Guid? impressoraId, byte[] dados, TipoDocumentoImpressao tipo, CancellationToken ct = default)
    {
        Impressora? impressora = null;
        if (impressoraId.HasValue)
        {
            impressora = await _context.Impressoras.FindAsync(new object[] { impressoraId.Value }, ct);
        }
        else
        {
            impressora = await _context.Impressoras.FirstOrDefaultAsync(i => i.Ativa, ct);
        }

        var trabalho = new TrabalhoImpressao
        {
            ImpressoraId = impressora?.Id,
            TipoDocumento = tipo,
            ConteudoBytes = dados,
            Estado = EstadoTrabalhoImpressao.Pendente,
            CriadoEm = DateTime.UtcNow
        };

        _context.TrabalhosImpressao.Add(trabalho);
        await _context.SaveChangesAsync(ct);

        if (impressora == null || !impressora.Ativa)
        {
            trabalho.Estado = EstadoTrabalhoImpressao.Falhado;
            trabalho.MensagemErro = "Nenhuma impressora ativa configurada.";
            await _context.SaveChangesAsync(ct);
            return false;
        }

        try
        {
            using var tcpClient = new TcpClient();
            var connectTask = tcpClient.ConnectAsync(impressora.EnderecoIp, impressora.Porta);
            var timeoutTask = Task.Delay(2000, ct);

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            if (completedTask == timeoutTask)
            {
                throw new TimeoutException($"Tempo limite excedido ao conectar à impressora {impressora.EnderecoIp}:{impressora.Porta}");
            }

            using var stream = tcpClient.GetStream();
            await stream.WriteAsync(dados, 0, dados.Length, ct);
            await stream.FlushAsync(ct);

            trabalho.Estado = EstadoTrabalhoImpressao.Enviado;
            trabalho.EnviadoEm = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Documento {Tipo} enviado com sucesso para a impressora {Ip}:{Porta}", tipo, impressora.EnderecoIp, impressora.Porta);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Não foi possível enviar trabalho de impressão para {Ip}:{Porta}. Detalhe: {Erro}", impressora.EnderecoIp, impressora.Porta, ex.Message);
            trabalho.Estado = EstadoTrabalhoImpressao.Falhado;
            trabalho.MensagemErro = ex.Message;
            trabalho.Tentativas++;
            await _context.SaveChangesAsync(ct);
            return false;
        }
    }

    public byte[] GerarTalaoConta(ContaDto conta, bool eSegundaVia = false)
    {
        var rest = ObterDadosRestaurante();
        return _generator.GerarTalaoConta(conta, rest.Nome, rest.Nuit, rest.Endereco, eSegundaVia);
    }

    public byte[] GerarReciboPagamento(ContaDto conta, PagamentoDto pagamento, decimal saldoRestante, bool eSegundaVia = false)
    {
        var rest = ObterDadosRestaurante();
        return _generator.GerarReciboPagamento(conta, pagamento, saldoRestante, rest.Nome, rest.Nuit, rest.Endereco, eSegundaVia);
    }

    public byte[] GerarTalaoCozinha(TicketKdsDto ticket)
    {
        return _generator.GerarTalaoCozinha(ticket);
    }

    public byte[] GerarRelatorioX(RelatorioXDto relatorio)
    {
        return _generator.GerarRelatorioX(relatorio);
    }

    public byte[] GerarTalaoZ(DocumentoZDto docZ, bool eSegundaVia = false)
    {
        return _generator.GerarTalaoZ(docZ, eSegundaVia);
    }

    public byte[] GerarComprovativoMovimento(MovimentoCaixaDto movimento, string caixaNome)
    {
        return _generator.GerarComprovativoMovimento(movimento, caixaNome);
    }

    private (string Nome, string Nuit, string Endereco) ObterDadosRestaurante()
    {
        var rest = _context.Restaurante.FirstOrDefault();
        if (rest != null)
        {
            return (rest.Nome, rest.Nuit, rest.Morada ?? "Av. Julius Nyerere, Maputo, Moçambique");
        }
        return ("Menova Restaurante", "400123456", "Av. Julius Nyerere, Maputo, Moçambique");
    }
}
