using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Menova.Domain.Auditoria;
using Xunit;

namespace Menova.UnitTests;

public class BackupsAndAuditoriaTests
{
    private const string GenesisHash = "0000000000000000000000000000000000000000000000000000000000000000";

    [Fact]
    public void Auditoria_CadeiaCompleta_ValidaComSucesso()
    {
        var data1 = new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc);
        var hash1 = RegistoAuditoria.CalcularHash(
            GenesisHash, data1, "admin", "LOGIN", "utilizadores", "1", null, null, null, "corr-1");

        var data2 = new DateTime(2026, 10, 15, 10, 5, 0, DateTimeKind.Utc);
        var hash2 = RegistoAuditoria.CalcularHash(
            hash1, data2, "admin", "CONTA.ABRIR", "contas", "10", null, "{\"Mesa\":5}", "Abertura", "corr-2");

        var data3 = new DateTime(2026, 10, 15, 10, 15, 0, DateTimeKind.Utc);
        var hash3 = RegistoAuditoria.CalcularHash(
            hash2, data3, "admin", "PAGAMENTO.REGISTAR", "pagamentos", "101", null, "{\"Valor\":500}", "Numerário", "corr-3");

        var lista = new List<RegistoAuditoria>
        {
            new RegistoAuditoria { Id = 1, DataHora = data1, NomeUtilizadorSnapshot = "admin", Acao = "LOGIN", Entidade = "utilizadores", EntidadeId = "1", CorrelacaoId = "corr-1", HashAnterior = GenesisHash, Hash = hash1 },
            new RegistoAuditoria { Id = 2, DataHora = data2, NomeUtilizadorSnapshot = "admin", Acao = "CONTA.ABRIR", Entidade = "contas", EntidadeId = "10", ValoresDepois = "{\"Mesa\":5}", Motivo = "Abertura", CorrelacaoId = "corr-2", HashAnterior = hash1, Hash = hash2 },
            new RegistoAuditoria { Id = 3, DataHora = data3, NomeUtilizadorSnapshot = "admin", Acao = "PAGAMENTO.REGISTAR", Entidade = "pagamentos", EntidadeId = "101", ValoresDepois = "{\"Valor\":500}", Motivo = "Numerário", CorrelacaoId = "corr-3", HashAnterior = hash2, Hash = hash3 }
        };

        // Algoritmo de validação da cadeia
        string hashEsperadoAnterior = GenesisHash;
        bool cadeiaValida = true;

        foreach (var reg in lista)
        {
            if (reg.HashAnterior != hashEsperadoAnterior)
            {
                cadeiaValida = false;
                break;
            }

            var hashRecalculado = RegistoAuditoria.CalcularHash(
                reg.HashAnterior,
                reg.DataHora,
                reg.NomeUtilizadorSnapshot,
                reg.Acao,
                reg.Entidade,
                reg.EntidadeId,
                reg.ValoresAntes,
                reg.ValoresDepois,
                reg.Motivo,
                reg.CorrelacaoId);

            if (reg.Hash != hashRecalculado)
            {
                cadeiaValida = false;
                break;
            }

            hashEsperadoAnterior = reg.Hash;
        }

        cadeiaValida.Should().BeTrue();
    }

    [Fact]
    public void Auditoria_AdulteracaoEmValoresDepois_QuebraAssinatura()
    {
        var data = new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc);
        var hashOriginal = RegistoAuditoria.CalcularHash(
            GenesisHash, data, "admin", "PAGAMENTO", "pagamentos", "1", null, "{\"Valor\":1000}", "Pago", "corr-1");

        var reg = new RegistoAuditoria
        {
            Id = 1,
            DataHora = data,
            NomeUtilizadorSnapshot = "admin",
            Acao = "PAGAMENTO",
            Entidade = "pagamentos",
            EntidadeId = "1",
            ValoresDepois = "{\"Valor\":500}", // Alterado fraudulentamente de 1000 para 500
            Motivo = "Pago",
            CorrelacaoId = "corr-1",
            HashAnterior = GenesisHash,
            Hash = hashOriginal
        };

        var hashRecalculado = RegistoAuditoria.CalcularHash(
            reg.HashAnterior,
            reg.DataHora,
            reg.NomeUtilizadorSnapshot,
            reg.Acao,
            reg.Entidade,
            reg.EntidadeId,
            reg.ValoresAntes,
            reg.ValoresDepois,
            reg.Motivo,
            reg.CorrelacaoId);

        reg.Hash.Should().NotBe(hashRecalculado);
    }

    [Fact]
    public void Backups_CalculoHashSha256_DeveSerExatoEIdempotente()
    {
        var conteudoSimulado = "PGDUMP_HEADER_MENOVA_2026_DATABASE_RESTAURANTE_MOZAMBIQUE";
        var bytes = Encoding.UTF8.GetBytes(conteudoSimulado);

        var hash1 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var hash2 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        hash1.Length.Should().Be(64);
        hash1.Should().Be(hash2);
    }
}
