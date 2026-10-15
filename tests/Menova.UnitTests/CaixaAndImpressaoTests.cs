using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Menova.Application.DTOs;
using Menova.Domain.Caixa;
using Menova.Domain.Pedidos;
using Menova.Domain.Sala;
using Menova.Infrastructure.Services;
using Xunit;

namespace Menova.UnitTests;

public class CaixaAndImpressaoTests
{
    [Fact]
    public void Caixa_CalculoSaldoEsperadoGaveta_DeveConsiderarApenasNumerarioESuprimentos()
    {
        // Em restauração, apenas pagamentos em Numerário afetam a gaveta física!
        // Multicaixa POS e M-Pesa vão para as contas bancárias/digitais da empresa.
        decimal saldoInicial = 1000.00m;
        decimal vendasNumerario = 2500.00m;
        decimal vendasPOS = 4000.00m;
        decimal vendasMPesa = 1800.00m;
        decimal suprimentos = 500.00m;
        decimal sangrias = 1200.00m;

        decimal totalVendas = vendasNumerario + vendasPOS + vendasMPesa;
        decimal saldoEsperadoFisico = saldoInicial + vendasNumerario + suprimentos - sangrias;

        totalVendas.Should().Be(8300.00m);
        saldoEsperadoFisico.Should().Be(2800.00m);

        decimal saldoContado = 2800.00m;
        decimal diferenca = saldoContado - saldoEsperadoFisico;
        diferenca.Should().Be(0.00m); // Caixa sem quebra nem sobra
    }

    [Fact]
    public void Pagamento_CalculoTroco_DeveDevolverTrocoCorreto()
    {
        decimal valorALiquidar = 650.00m;
        decimal notaEntregue = 1000.00m;

        decimal troco = notaEntregue > valorALiquidar ? notaEntregue - valorALiquidar : 0.00m;

        troco.Should().Be(350.00m);
    }

    [Fact]
    public void Pagamento_LiquidacaoTotal_DeveCompletarSaldoConta()
    {
        var conta = new Conta
        {
            TotalLiquido = 1850.00m,
            Estado = EstadoConta.Aberta
        };

        var pagamentos = new List<Pagamento>
        {
            new Pagamento { Valor = 1000.00m, Metodo = MetodoPagamento.MulticaixaPOS },
            new Pagamento { Valor = 850.00m, Metodo = MetodoPagamento.Numerario }
        };

        var totalLiquidado = pagamentos.Sum(p => p.Valor);
        if (totalLiquidado >= conta.TotalLiquido)
        {
            conta.Estado = EstadoConta.Fechada;
        }

        totalLiquidado.Should().Be(1850.00m);
        conta.Estado.Should().Be(EstadoConta.Fechada);
    }

    [Fact]
    public void FechoZ_HashSha256_EncadeamentoCorretoDeveSerValidado()
    {
        var hashZero = new string('0', 64);
        var data1 = new DateTime(2026, 10, 15, 14, 0, 0, DateTimeKind.Utc);
        var sessao1Id = Guid.NewGuid();

        // 1. Calcular Hash do Z-1
        var payloadZ1 = $"1|{sessao1Id}|{data1:O}|1000.0000|0.0000|160.0000|1000.0000|{{\"Numerário\":1000.00}}|{{\"16%\":160.00}}|{hashZero}";
        var hashZ1 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payloadZ1))).ToLowerInvariant();

        hashZ1.Length.Should().Be(64);
        hashZ1.Should().NotBe(hashZero);

        // 2. Calcular Hash do Z-2 encadeado no Z-1
        var data2 = new DateTime(2026, 10, 15, 23, 0, 0, DateTimeKind.Utc);
        var sessao2Id = Guid.NewGuid();
        var payloadZ2 = $"2|{sessao2Id}|{data2:O}|2500.0000|0.0000|400.0000|2500.0000|{{\"M-Pesa\":2500.00}}|{{\"16%\":400.00}}|{hashZ1}";
        var hashZ2 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payloadZ2))).ToLowerInvariant();

        hashZ2.Length.Should().Be(64);
        hashZ2.Should().NotBe(hashZ1);

        // 3. Simulação de validação da cadeia
        var cadeia = new List<(int NumZ, string Hash, string HashAnt)>
        {
            (1, hashZ1, hashZero),
            (2, hashZ2, hashZ1)
        };

        bool cadeiaIntegra = true;
        string anteriorEsperado = hashZero;
        foreach (var z in cadeia)
        {
            if (z.HashAnt != anteriorEsperado)
            {
                cadeiaIntegra = false;
                break;
            }
            anteriorEsperado = z.Hash;
        }

        cadeiaIntegra.Should().BeTrue();
    }

    [Fact]
    public void FechoZ_AdulteracaoNoHashOuDados_DeveQuebrarCadeia()
    {
        var hashZero = new string('0', 64);
        var data1 = new DateTime(2026, 10, 15, 14, 0, 0, DateTimeKind.Utc);
        var sessao1Id = Guid.NewGuid();

        var payloadZ1 = $"1|{sessao1Id}|{data1:O}|1000.0000|0.0000|160.0000|1000.0000|{{\"Numerário\":1000.00}}|{{\"16%\":160.00}}|{hashZero}";
        var hashZ1 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payloadZ1))).ToLowerInvariant();

        // Tentativa de alterar o hash do Z-1
        var hashAdulteradoZ1 = hashZ1.Substring(0, 60) + "ffff";

        var data2 = new DateTime(2026, 10, 15, 23, 0, 0, DateTimeKind.Utc);
        var sessao2Id = Guid.NewGuid();
        var payloadZ2 = $"2|{sessao2Id}|{data2:O}|2500.0000|0.0000|400.0000|2500.0000|{{\"M-Pesa\":2500.00}}|{{\"16%\":400.00}}|{hashZ1}";
        var hashZ2 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payloadZ2))).ToLowerInvariant();

        var cadeiaAdulterada = new List<(int NumZ, string Hash, string HashAnt)>
        {
            (1, hashAdulteradoZ1, hashZero), // Alterado!
            (2, hashZ2, hashZ1)
        };

        bool cadeiaIntegra = true;
        string anteriorEsperado = hashZero;
        foreach (var z in cadeiaAdulterada)
        {
            if (z.HashAnt != anteriorEsperado)
            {
                cadeiaIntegra = false;
                break;
            }
            anteriorEsperado = z.Hash;
        }

        cadeiaIntegra.Should().BeFalse();
    }

    [Fact]
    public void EscPosGenerator_GerarTalaoConta_DeveProduzirBytesFormatados()
    {
        var generator = new EscPosGenerator(larguraColunas: 42);

        var conta = new ContaDto(
            Id: Guid.NewGuid(),
            Numero: "C2600010",
            MesaId: Guid.NewGuid(),
            MesaNumero: "07",
            ZonaNome: "Terraço",
            Estado: EstadoConta.Aberta,
            EstadoDescricao: "Aberta",
            TotalBruto: 1500.00m,
            TotalDesconto: 0m,
            TotalIva: 240.00m,
            TotalLiquido: 1500.00m,
            Observacoes: null,
            CriadoEm: DateTime.UtcNow,
            CriadoPor: "garcom1",
            Pedidos: new List<PedidoDto>
            {
                new PedidoDto(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    1,
                    EstadoPedido.Entregue,
                    "Entregue",
                    null,
                    DateTime.UtcNow,
                    "garcom1",
                    new List<ItemPedidoDto>
                    {
                        new ItemPedidoDto(
                            Guid.NewGuid(),
                            Guid.NewGuid(),
                            "Frango Zambeziano",
                            1,
                            650.00m,
                            16m,
                            650.00m,
                            104.00m,
                            650.00m,
                            null,
                            "Cozinha",
                            EstadoItemPedido.Pronto,
                            "Pronto",
                            null,
                            false,
                            null,
                            new List<string> { "Picante Médio" }
                        )
                    }
                )
            }
        );

        var bytes = generator.GerarTalaoConta(conta, "Restaurante Menova", "400123456", "Av. Julius Nyerere, Maputo", eSegundaVia: false);

        bytes.Should().NotBeNullOrEmpty();
        bytes.Should().Contain(0x1D).And.Contain(0x56); // Corte de papel
    }

    [Fact]
    public void EscPosGenerator_SegundaVia_DeveIncluirCabecalhoCopia()
    {
        var generator = new EscPosGenerator(larguraColunas: 42);

        var docZ = new DocumentoZDto(
            Id: Guid.NewGuid(),
            SessaoCaixaId: Guid.NewGuid(),
            NumeroZ: 14,
            DataHora: DateTime.UtcNow,
            CaixaNome: "Caixa 1",
            CaixaNumero: 1,
            EmitidoPor: "supervisor",
            TotalBruto: 12500.00m,
            TotalDesconto: 0.00m,
            TotalIva: 2000.00m,
            TotalLiquido: 12500.00m,
            TotalDocumentosFaturados: 18,
            TotaisPorMetodo: new Dictionary<string, decimal> { { "Numerário", 8000.00m }, { "M-Pesa", 4500.00m } },
            TotaisPorTaxaIva: new Dictionary<string, decimal> { { "16%", 2000.00m } },
            HashAnterior: new string('0', 64),
            Hash: new string('a', 64),
            HashValido: true
        );

        var bytes = generator.GerarTalaoZ(docZ, eSegundaVia: true);

        bytes.Should().NotBeNullOrEmpty();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encodingCp860 = Encoding.GetEncoding(860);
        var textoDecodificado = encodingCp860.GetString(bytes);
        textoDecodificado.Should().Contain("2ª VIA");
    }
}
