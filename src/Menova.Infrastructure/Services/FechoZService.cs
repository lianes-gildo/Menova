using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Menova.Application.DTOs;
using Menova.Application.Interfaces;
using Menova.Domain.Caixa;
using Microsoft.EntityFrameworkCore;

namespace Menova.Infrastructure.Services;

public class FechoZService : IFechoZService
{
    private readonly IAppDbContext _context;
    private readonly IAuditoriaService _auditoriaService;

    public FechoZService(IAppDbContext context, IAuditoriaService auditoriaService)
    {
        _context = context;
        _auditoriaService = auditoriaService;
    }

    public async Task<DocumentoZDto> EmitirFechoZAsync(Guid sessaoCaixaId, string utilizadorNome, CancellationToken ct = default)
    {
        var sessao = await _context.SessoesCaixa
            .Include(s => s.Caixa)
            .Include(s => s.Pagamentos).ThenInclude(p => p.Conta)
            .FirstOrDefaultAsync(s => s.Id == sessaoCaixaId, ct);

        if (sessao == null)
        {
            throw new InvalidOperationException("Sessão de caixa não encontrada.");
        }

        // 1. Obter número sequencial Z contínuo
        var ultimoNumeroZ = await _context.DocumentosZ
            .OrderByDescending(z => z.NumeroZ)
            .Select(z => z.NumeroZ)
            .FirstOrDefaultAsync(ct);

        var novoNumeroZ = ultimoNumeroZ + 1;

        // 2. Obter hash anterior para encadeamento WORM
        var ultimoZ = await _context.DocumentosZ
            .OrderByDescending(z => z.NumeroZ)
            .FirstOrDefaultAsync(ct);

        var hashAnterior = ultimoZ?.Hash ?? new string('0', 64);

        // 3. Totais financeiros
        var contasPagas = sessao.Pagamentos.Select(p => p.Conta).Distinct().ToList();
        var totalBruto = contasPagas.Sum(c => c.TotalBruto);
        var totalDesconto = contasPagas.Sum(c => c.TotalDesconto);
        var totalIva = contasPagas.Sum(c => c.TotalIva);
        var totalLiquido = sessao.Pagamentos.Sum(p => p.Valor);

        var totaisPorMetodoDict = sessao.Pagamentos
            .GroupBy(p => p.Metodo)
            .ToDictionary(g => TraduzirMetodo(g.Key), g => g.Sum(p => p.Valor));

        var totaisPorTaxaIvaDict = new Dictionary<string, decimal>
        {
            { "16% (Normal)", totalIva }
        };

        var totaisMetodoJson = JsonSerializer.Serialize(totaisPorMetodoDict);
        var totaisIvaJson = JsonSerializer.Serialize(totaisPorTaxaIvaDict);
        var dataHora = DateTime.UtcNow;

        // 4. Calcular Hash SHA-256 encadeado
        var payloadParaHash = $"{novoNumeroZ}|{sessaoCaixaId}|{dataHora:O}|{totalBruto:F4}|{totalDesconto:F4}|{totalIva:F4}|{totalLiquido:F4}|{totaisMetodoJson}|{totaisIvaJson}|{hashAnterior}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(payloadParaHash));
        var hashCalculado = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var documentoZ = new DocumentoZ
        {
            SessaoCaixaId = sessaoCaixaId,
            NumeroZ = novoNumeroZ,
            DataHora = dataHora,
            TotalBruto = totalBruto,
            TotalDesconto = totalDesconto,
            TotalIva = totalIva,
            TotalLiquido = totalLiquido,
            TotaisPorMetodo = totaisMetodoJson,
            TotaisPorTaxaIva = totaisIvaJson,
            TotalDocumentosFaturados = contasPagas.Count,
            EmitidoPor = utilizadorNome,
            HashAnterior = hashAnterior,
            Hash = hashCalculado
        };

        _context.DocumentosZ.Add(documentoZ);

        // Se a sessão ainda estiver aberta, encerra-a
        if (sessao.Estado == EstadoSessaoCaixa.Aberta)
        {
            sessao.Estado = EstadoSessaoCaixa.Fechada;
            sessao.FechadaEm = dataHora;
        }

        await _context.SaveChangesAsync(ct);

        await _auditoriaService.RegistarAsync(
            acao: "FECHO_Z.EMITIR",
            entidade: "documentos_z",
            entidadeId: documentoZ.Id.ToString(),
            valoresDepois: new { documentoZ.NumeroZ, documentoZ.TotalLiquido, documentoZ.Hash, documentoZ.EmitidoPor },
            motivo: $"Emissão do Talão Z nº {documentoZ.NumeroZ} por {utilizadorNome}",
            cancellationToken: ct);

        return new DocumentoZDto(
            documentoZ.Id,
            documentoZ.SessaoCaixaId,
            documentoZ.NumeroZ,
            documentoZ.DataHora,
            sessao.Caixa.Nome,
            sessao.Caixa.Numero,
            documentoZ.EmitidoPor,
            documentoZ.TotalBruto,
            documentoZ.TotalDesconto,
            documentoZ.TotalIva,
            documentoZ.TotalLiquido,
            documentoZ.TotalDocumentosFaturados,
            totaisPorMetodoDict,
            totaisPorTaxaIvaDict,
            documentoZ.HashAnterior,
            documentoZ.Hash,
            true
        );
    }

    public async Task<DocumentoZDto?> ObterZPorIdAsync(Guid documentoZId, CancellationToken ct = default)
    {
        var z = await _context.DocumentosZ
            .AsNoTracking()
            .Include(doc => doc.SessaoCaixa).ThenInclude(s => s.Caixa)
            .FirstOrDefaultAsync(doc => doc.Id == documentoZId, ct);

        if (z == null) return null;

        var metodos = JsonSerializer.Deserialize<Dictionary<string, decimal>>(z.TotaisPorMetodo) ?? new();
        var taxasIva = JsonSerializer.Deserialize<Dictionary<string, decimal>>(z.TotaisPorTaxaIva) ?? new();
        var valido = VerificarHashIndividual(z);

        return new DocumentoZDto(
            z.Id,
            z.SessaoCaixaId,
            z.NumeroZ,
            z.DataHora,
            z.SessaoCaixa.Caixa.Nome,
            z.SessaoCaixa.Caixa.Numero,
            z.EmitidoPor,
            z.TotalBruto,
            z.TotalDesconto,
            z.TotalIva,
            z.TotalLiquido,
            z.TotalDocumentosFaturados,
            metodos,
            taxasIva,
            z.HashAnterior,
            z.Hash,
            valido
        );
    }

    public async Task<List<DocumentoZDto>> ObterHistoricoZAsync(CancellationToken ct = default)
    {
        var lista = await _context.DocumentosZ
            .AsNoTracking()
            .Include(z => z.SessaoCaixa).ThenInclude(s => s.Caixa)
            .OrderByDescending(z => z.NumeroZ)
            .ToListAsync(ct);

        return lista.Select(z =>
        {
            var metodos = JsonSerializer.Deserialize<Dictionary<string, decimal>>(z.TotaisPorMetodo) ?? new();
            var taxasIva = JsonSerializer.Deserialize<Dictionary<string, decimal>>(z.TotaisPorTaxaIva) ?? new();
            var valido = VerificarHashIndividual(z);

            return new DocumentoZDto(
                z.Id,
                z.SessaoCaixaId,
                z.NumeroZ,
                z.DataHora,
                z.SessaoCaixa?.Caixa?.Nome ?? "Caixa",
                z.SessaoCaixa?.Caixa?.Numero ?? 1,
                z.EmitidoPor,
                z.TotalBruto,
                z.TotalDesconto,
                z.TotalIva,
                z.TotalLiquido,
                z.TotalDocumentosFaturados,
                metodos,
                taxasIva,
                z.HashAnterior,
                z.Hash,
                valido
            );
        }).ToList();
    }

    public async Task<bool> VerificarIntegridadeCadeiaAsync(CancellationToken ct = default)
    {
        var documentos = await _context.DocumentosZ
            .AsNoTracking()
            .OrderBy(z => z.NumeroZ)
            .ToListAsync(ct);

        if (!documentos.Any()) return true;

        string hashEsperadoAnterior = new string('0', 64);

        foreach (var z in documentos)
        {
            // 1. Validar encadeamento com o anterior
            if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(z.HashAnterior),
                Encoding.UTF8.GetBytes(hashEsperadoAnterior)))
            {
                return false;
            }

            // 2. Recalcular o hash do próprio documento
            var payload = $"{z.NumeroZ}|{z.SessaoCaixaId}|{z.DataHora:O}|{z.TotalBruto:F4}|{z.TotalDesconto:F4}|{z.TotalIva:F4}|{z.TotalLiquido:F4}|{z.TotaisPorMetodo}|{z.TotaisPorTaxaIva}|{z.HashAnterior}";
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
            var hashCalculado = Convert.ToHexString(hashBytes).ToLowerInvariant();

            if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(z.Hash),
                Encoding.UTF8.GetBytes(hashCalculado)))
            {
                return false;
            }

            hashEsperadoAnterior = z.Hash;
        }

        return true;
    }

    private static bool VerificarHashIndividual(DocumentoZ z)
    {
        var payload = $"{z.NumeroZ}|{z.SessaoCaixaId}|{z.DataHora:O}|{z.TotalBruto:F4}|{z.TotalDesconto:F4}|{z.TotalIva:F4}|{z.TotalLiquido:F4}|{z.TotaisPorMetodo}|{z.TotaisPorTaxaIva}|{z.HashAnterior}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        var hashCalculado = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(z.Hash),
            Encoding.UTF8.GetBytes(hashCalculado));
    }

    private static string TraduzirMetodo(MetodoPagamento metodo) => metodo switch
    {
        MetodoPagamento.Numerario => "Numerário",
        MetodoPagamento.MulticaixaPOS => "Multicaixa POS",
        MetodoPagamento.MPesaManual => "M-Pesa",
        MetodoPagamento.EMolaManual => "e-Mola",
        MetodoPagamento.Transferencia => "Transferência",
        MetodoPagamento.Cheque => "Cheque",
        _ => "Outro"
    };
}
