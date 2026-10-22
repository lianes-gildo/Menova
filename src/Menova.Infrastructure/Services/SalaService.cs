using Menova.Application.DTOs;
using Menova.Application.Interfaces;
using Menova.Domain.Sala;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Menova.Infrastructure.Services;

public class SalaService : ISalaService
{
    private readonly IAppDbContext _context;
    private readonly IAuditoriaService _auditoria;
    private readonly INotificacaoService _notificacao;
    private readonly ILogger<SalaService> _logger;

    public SalaService(
        IAppDbContext context,
        IAuditoriaService auditoria,
        INotificacaoService notificacao,
        ILogger<SalaService> logger)
    {
        _context = context;
        _auditoria = auditoria;
        _notificacao = notificacao;
        _logger = logger;
    }

    public async Task<List<ZonaDto>> GetZonasComMesasAsync(CancellationToken cancellationToken = default)
    {
        var zonas = await _context.Zonas
            .AsNoTracking()
            .Include(z => z.Mesas)
            .Where(z => z.Ativa)
            .OrderBy(z => z.Ordem)
            .ToListAsync(cancellationToken);

        var resultado = new List<ZonaDto>();

        foreach (var z in zonas)
        {
            var mesasAtivas = z.Mesas.Where(m => m.Ativa).OrderBy(m => m.Numero).ToList();
            var mesasDtos = mesasAtivas.Select(m => new MesaDto(
                m.Id,
                m.ZonaId,
                z.Nome,
                m.Numero,
                m.Lugares,
                m.PosicaoX,
                m.PosicaoY,
                m.Estado,
                ObterDescricaoEstado(m.Estado),
                m.Ativa,
                null,
                null,
                null
            )).ToList();

            var total = mesasDtos.Count;
            var ocupadas = mesasDtos.Count(m => m.Estado == EstadoMesa.Ocupada || m.Estado == EstadoMesa.ContaPedida);

            resultado.Add(new ZonaDto(
                z.Id,
                z.Nome,
                z.Ordem,
                z.Ativa,
                total,
                ocupadas,
                mesasDtos));
        }

        return resultado;
    }

    public async Task<MesaDto?> GetMesaPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var m = await _context.Mesas
            .AsNoTracking()
            .Include(m => m.Zona)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (m == null) return null;

        return new MesaDto(
            m.Id,
            m.ZonaId,
            m.Zona.Nome,
            m.Numero,
            m.Lugares,
            m.PosicaoX,
            m.PosicaoY,
            m.Estado,
            ObterDescricaoEstado(m.Estado),
            m.Ativa,
            null,
            null,
            null
        );
    }

    public async Task AtualizarEstadoMesaAsync(Guid mesaId, EstadoMesa novoEstado, string? motivo = null, CancellationToken cancellationToken = default)
    {
        var mesa = await _context.Mesas.FirstOrDefaultAsync(m => m.Id == mesaId, cancellationToken)
            ?? throw new KeyNotFoundException($"Mesa com ID {mesaId} não encontrada.");

        var estadoAnterior = mesa.Estado;
        if (estadoAnterior == novoEstado) return;

        mesa.Estado = novoEstado;
        await _context.SaveChangesAsync(cancellationToken);

        await _auditoria.RegistarAsync(
            acao: "MESA.ALTERAR_ESTADO",
            entidade: "mesas",
            entidadeId: mesa.Id.ToString(),
            valoresAntes: new { Estado = estadoAnterior },
            valoresDepois: new { Estado = novoEstado },
            motivo: motivo ?? $"Transição de estado da Mesa {mesa.Numero} para {novoEstado}",
            cancellationToken: cancellationToken);

        await _notificacao.NotificarMesaAlteradaAsync(mesa.Id, novoEstado, mesa.Numero, cancellationToken);

        _logger.LogInformation("Mesa {Numero} alterou de estado para {NovoEstado}", mesa.Numero, novoEstado);
    }

    public async Task<Guid> CriarMesaAsync(CriarMesaDto dto, CancellationToken cancellationToken = default)
    {
        var zona = await _context.Zonas.FindAsync(new object[] { dto.ZonaId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Zona {dto.ZonaId} não encontrada.");

        var mesa = new Mesa
        {
            ZonaId = dto.ZonaId,
            Numero = dto.Numero.Trim(),
            Lugares = dto.Lugares,
            PosicaoX = dto.PosicaoX,
            PosicaoY = dto.PosicaoY,
            Estado = EstadoMesa.Livre,
            Ativa = true
        };

        _context.Mesas.Add(mesa);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditoria.RegistarAsync(
            acao: "MESA.CRIAR",
            entidade: "mesas",
            entidadeId: mesa.Id.ToString(),
            valoresDepois: mesa,
            cancellationToken: cancellationToken);

        return mesa.Id;
    }

    public async Task AtualizarMesaAsync(AtualizarMesaDto dto, CancellationToken cancellationToken = default)
    {
        var mesa = await _context.Mesas.FirstOrDefaultAsync(m => m.Id == dto.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Mesa {dto.Id} não encontrada.");

        mesa.ZonaId = dto.ZonaId;
        mesa.Numero = dto.Numero.Trim();
        mesa.Lugares = dto.Lugares;
        mesa.PosicaoX = dto.PosicaoX;
        mesa.PosicaoY = dto.PosicaoY;
        mesa.Ativa = dto.Ativa;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditoria.RegistarAsync(
            acao: "MESA.ATUALIZAR",
            entidade: "mesas",
            entidadeId: mesa.Id.ToString(),
            valoresDepois: mesa,
            cancellationToken: cancellationToken);
    }

    public async Task EliminarMesaAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var mesa = await _context.Mesas.FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Mesa {id} não encontrada.");

        _context.Mesas.Remove(mesa);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditoria.RegistarAsync(
            acao: "MESA.ELIMINAR",
            entidade: "mesas",
            entidadeId: id.ToString(),
            valoresAntes: new { mesa.Numero, mesa.ZonaId },
            cancellationToken: cancellationToken);
    }

    public async Task<Guid> CriarZonaAsync(CriarZonaDto dto, CancellationToken cancellationToken = default)
    {
        var zona = new Zona
        {
            Nome = dto.Nome.Trim(),
            Ordem = dto.Ordem,
            Ativa = true
        };

        _context.Zonas.Add(zona);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditoria.RegistarAsync(
            acao: "ZONA.CRIAR",
            entidade: "zonas",
            entidadeId: zona.Id.ToString(),
            valoresDepois: zona,
            cancellationToken: cancellationToken);

        return zona.Id;
    }

    public async Task AtualizarZonaAsync(AtualizarZonaDto dto, CancellationToken cancellationToken = default)
    {
        var zona = await _context.Zonas.FirstOrDefaultAsync(z => z.Id == dto.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Zona {dto.Id} não encontrada.");

        var nomeAnterior = zona.Nome;
        zona.Nome = dto.Nome.Trim();
        zona.Ordem = dto.Ordem;
        zona.Ativa = dto.Ativa;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditoria.RegistarAsync(
            acao: "ZONA.ATUALIZAR",
            entidade: "zonas",
            entidadeId: zona.Id.ToString(),
            valoresAntes: new { Nome = nomeAnterior },
            valoresDepois: new { zona.Nome, zona.Ordem, zona.Ativa },
            cancellationToken: cancellationToken);
    }

    public async Task EliminarZonaAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var zona = await _context.Zonas.Include(z => z.Mesas).FirstOrDefaultAsync(z => z.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Zona {id} não encontrada.");

        if (zona.Mesas.Any(m => m.Ativa && (m.Estado == EstadoMesa.Ocupada || m.Estado == EstadoMesa.ContaPedida)))
        {
            throw new InvalidOperationException("Não é possível remover um salão/zona com mesas ocupadas ou com contas em aberto.");
        }

        zona.Ativa = false;
        foreach (var m in zona.Mesas)
        {
            m.Ativa = false;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditoria.RegistarAsync(
            acao: "ZONA.DESATIVAR",
            entidade: "zonas",
            entidadeId: zona.Id.ToString(),
            valoresAntes: new { zona.Nome },
            cancellationToken: cancellationToken);
    }

    public async Task<ZonaDto?> GetZonaPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var z = await _context.Zonas
            .AsNoTracking()
            .Include(z => z.Mesas)
            .FirstOrDefaultAsync(z => z.Id == id, cancellationToken);

        if (z == null) return null;

        var mesasDtos = z.Mesas.Where(m => m.Ativa).Select(m => new MesaDto(
            m.Id,
            m.ZonaId,
            z.Nome,
            m.Numero,
            m.Lugares,
            m.PosicaoX,
            m.PosicaoY,
            m.Estado,
            ObterDescricaoEstado(m.Estado),
            m.Ativa,
            null,
            null,
            null
        )).ToList();

        return new ZonaDto(
            z.Id,
            z.Nome,
            z.Ordem,
            z.Ativa,
            mesasDtos.Count,
            mesasDtos.Count(m => m.Estado == EstadoMesa.Ocupada || m.Estado == EstadoMesa.ContaPedida),
            mesasDtos);
    }

    private static string ObterDescricaoEstado(EstadoMesa estado) => estado switch
    {
        EstadoMesa.Livre => "Livre",
        EstadoMesa.Ocupada => "Ocupada",
        EstadoMesa.ContaPedida => "Conta Pedida",
        EstadoMesa.Reservada => "Reservada",
        EstadoMesa.Bloqueada => "Bloqueada",
        _ => "Desconhecido"
    };
}
