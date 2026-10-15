using FluentAssertions;
using Menova.Domain.Auditoria;
using Xunit;

namespace Menova.UnitTests;

public class AuditoriaHashTests
{
    [Fact]
    public void CalcularHash_ComMesmosDados_DeveProduzirMesmoHash()
    {
        var dataHora = new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);
        var anterior = "0000000000000000000000000000000000000000000000000000000000000000";

        var hash1 = RegistoAuditoria.CalcularHash(
            anterior, dataHora, "admin", "login.sucesso", "utilizadores", "1", null, "{\"ip\":\"127.0.0.1\"}", null, "corr-1");

        var hash2 = RegistoAuditoria.CalcularHash(
            anterior, dataHora, "admin", "login.sucesso", "utilizadores", "1", null, "{\"ip\":\"127.0.0.1\"}", null, "corr-1");

        hash1.Should().NotBeNullOrWhiteSpace();
        hash1.Length.Should().Be(64); // SHA-256 Hex
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void CalcularHash_ComAlteracaoEmQualquerCampo_DeveProduzirHashDiferente()
    {
        var dataHora = new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);
        var anterior = "0000000000000000000000000000000000000000000000000000000000000000";

        var original = RegistoAuditoria.CalcularHash(
            anterior, dataHora, "admin", "item.anular", "pedido_itens", "101", "{\"preco\":500}", null, "Engano do cliente", "corr-1");

        var alterado = RegistoAuditoria.CalcularHash(
            anterior, dataHora, "admin", "item.anular", "pedido_itens", "101", "{\"preco\":400}", null, "Engano do cliente", "corr-1");

        original.Should().NotBe(alterado);
    }

    [Fact]
    public void CadeiaDeAuditoria_QuandoInterrompida_DeveSerDetetada()
    {
        var anterior = "0000000000000000000000000000000000000000000000000000000000000000";
        var chain = new List<RegistoAuditoria>();

        for (int i = 1; i <= 3; i++)
        {
            var data = DateTime.UtcNow.AddMinutes(i);
            var hash = RegistoAuditoria.CalcularHash(anterior, data, "admin", $"acao.{i}", "entidade", i.ToString(), null, null, null, null);
            var item = new RegistoAuditoria
            {
                Id = i,
                DataHora = data,
                NomeUtilizadorSnapshot = "admin",
                Acao = $"acao.{i}",
                Entidade = "entidade",
                EntidadeId = i.ToString(),
                HashAnterior = anterior,
                Hash = hash
            };
            chain.Add(item);
            anterior = hash;
        }

        // Validação da cadeia original íntegra
        for (int i = 0; i < chain.Count; i++)
        {
            if (i > 0)
            {
                chain[i].HashAnterior.Should().Be(chain[i - 1].Hash);
            }
        }

        // Adulterar o registo #2
        chain[1].Acao = "acao.adulterada";

        var hashRecalculado = RegistoAuditoria.CalcularHash(
            chain[1].HashAnterior,
            chain[1].DataHora,
            chain[1].NomeUtilizadorSnapshot,
            chain[1].Acao,
            chain[1].Entidade,
            chain[1].EntidadeId,
            chain[1].ValoresAntes,
            chain[1].ValoresDepois,
            chain[1].Motivo,
            chain[1].CorrelacaoId);

        hashRecalculado.Should().NotBe(chain[1].Hash);
    }
}
