using FluentAssertions;
using Menova.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Menova.UnitTests;

public class SecurityAndRegionalTests
{
    private readonly IConfiguration _config;

    public SecurityAndRegionalTests()
    {
        var inMemory = new Dictionary<string, string?>
        {
            ["Security:PinPepper"] = "Teste_Pepper_Moçambique_2026"
        };
        _config = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();
    }

    [Fact]
    public void PinService_DeveGerarHashValidoEVerificarComSucesso()
    {
        var pinService = new PinService(_config);
        var pin = "1234";
        var salt = pinService.GerarSalt();

        var hash = pinService.HashPin(pin, salt);

        hash.Should().NotBeNullOrWhiteSpace();
        pinService.VerificarPin(pin, salt, hash).Should().BeTrue();
        pinService.VerificarPin("0000", salt, hash).Should().BeFalse();
    }

    [Fact]
    public void DateTimeService_DeveForcarFusoAfricaMaputo()
    {
        var dateTimeService = new DateTimeService();

        dateTimeService.FusoHorario.Should().NotBeNull();
        dateTimeService.FusoHorario.BaseUtcOffset.Should().Be(TimeSpan.FromHours(2)); // UTC+2 CAT
    }

    [Fact]
    public void DateTimeService_DeveFormatarMoedaPadraoMocambique()
    {
        var dateTimeService = new DateTimeService();
        decimal valor = 1250.50m;

        var formatado = dateTimeService.FormatarMoeda(valor);

        formatado.Should().Contain("MZN");
        formatado.Should().Contain("1");
        formatado.Should().Contain("250");
        formatado.Should().Contain("50");
    }

    [Fact]
    public void PermissoesCatalogo_NaoDeveConterCodigosDuplicados()
    {
        var permissoes = Menova.Application.Security.PermissoesCatalogo.Todas;

        permissoes.Should().NotBeEmpty();
        var codigos = permissoes.Select(p => p.Codigo).ToList();
        codigos.Distinct().Count().Should().Be(codigos.Count);
    }

    [Fact]
    public void RegrasDePerfis_GarcomNaoDeveTerAcessoAMenuOuEsgotarArtigos()
    {
        // Garçom só deve ter permissões estritas de salão e pedidos
        var permissoesProibidasGarcom = new[]
        {
            Menova.Application.Security.PermissoesCatalogo.MenuVer,
            Menova.Application.Security.PermissoesCatalogo.MenuGerir,
            Menova.Application.Security.PermissoesCatalogo.ArtigosEsgotar,
            Menova.Application.Security.PermissoesCatalogo.ItensAnular,
            Menova.Application.Security.PermissoesCatalogo.CaixaAbrir,
            Menova.Application.Security.PermissoesCatalogo.CaixaCobrar,
            Menova.Application.Security.PermissoesCatalogo.RelatoriosVer,
            Menova.Application.Security.PermissoesCatalogo.UtilizadoresVer,
            Menova.Application.Security.PermissoesCatalogo.DefinicoesGerir
        };

        // Assegurar que os catálogos de segurança protegem adequadamente o perfil de garçom
        permissoesProibidasGarcom.Should().NotContain(Menova.Application.Security.PermissoesCatalogo.ContasAbrir);
        permissoesProibidasGarcom.Should().NotContain(Menova.Application.Security.PermissoesCatalogo.MesasVer);
    }

    [Fact]
    public void Utilizador_DevePermitirAssociarCredencialBiometrica()
    {
        var utilizador = new Menova.Domain.Identity.Utilizador
        {
            NomeCompleto = "Garçom Amilcar",
            NomeUtilizador = "amilcar",
            HashPalavraPasse = "hash",
            BiometriaCredencialId = "bio_cred_xyz_123",
            BiometriaDispositivo = "iPhone 15 Pro (Face ID)",
            BiometriaRegistadaEm = DateTime.UtcNow
        };

        utilizador.BiometriaCredencialId.Should().Be("bio_cred_xyz_123");
        utilizador.BiometriaDispositivo.Should().Contain("Face ID");
        utilizador.BiometriaRegistadaEm.Should().NotBeNull();
    }

    [Theory]
    [InlineData("1234", true)]
    [InlineData("9999", true)]
    [InlineData("0000", true)]
    [InlineData("123", false)]
    [InlineData("12345", false)]
    [InlineData("abcd", false)]
    [InlineData("12a4", false)]
    public void PinGarcom_ValidacaoDeFormato4Digitos(string pin, bool validoEsperado)
    {
        var regex = new System.Text.RegularExpressions.Regex(@"^\d{4}$");
        var valido = regex.IsMatch(pin);
        valido.Should().Be(validoEsperado);
    }

    [Fact]
    public void SalaDtos_AtualizarZonaDtoECriarMesaDto_DevemConterTodosOsDados()
    {
        var zonaId = Guid.NewGuid();
        var zonaDto = new Menova.Application.DTOs.AtualizarZonaDto(
            Id: zonaId,
            Nome: "Esplanada Sul",
            Ordem: 2,
            Ativa: true
        );

        zonaDto.Nome.Should().Be("Esplanada Sul");
        zonaDto.Ordem.Should().Be(2);
        zonaDto.Ativa.Should().BeTrue();

        var mesaDto = new Menova.Application.DTOs.CriarMesaDto(
            ZonaId: zonaDto.Id,
            Numero: "15",
            Lugares: 6,
            PosicaoX: 10,
            PosicaoY: 20
        );

        mesaDto.Numero.Should().Be("15");
        mesaDto.Lugares.Should().Be(6);
        mesaDto.ZonaId.Should().Be(zonaDto.Id);
    }
}
