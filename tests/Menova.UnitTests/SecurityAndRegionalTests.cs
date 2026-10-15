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
}
