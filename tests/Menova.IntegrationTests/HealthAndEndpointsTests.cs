using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Menova.IntegrationTests;

public class HealthAndEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthAndEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthLive_DeveRetornar200EStatusHealthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
        content.Should().Contain("Menova POS");
    }

    [Fact]
    public async Task Login_DeveRetornarPaginaInicialEmPtPt()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/auth/login");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Acesso ao Sistema");
        content.Should().Contain("Nome de utilizador");
        content.Should().Contain("Palavra-passe");
        content.Should().Contain("menova_logo_0f.png");
    }

    [Fact]
    public async Task Erro404_DeveRetornar404ComTextoAmigavelPtPt()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/rota-inexistente-xyz");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Página Não Encontrada");
    }

    [Fact]
    public async Task InstalarCertificado_DeveRetornarInstrucoesHttpsLocal()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/instalar-certificado");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Certificado de Rede Local");
        content.Should().Contain("Android");
        content.Should().Contain("iOS");
        content.Should().Contain("Windows");
    }

    [Fact]
    public async Task RotasProtegidas_SemAutenticacao_DevemRedirecionarParaLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var responseUtilizadores = await client.GetAsync("/utilizadores");
        responseUtilizadores.StatusCode.Should().Be(HttpStatusCode.Redirect);
        responseUtilizadores.Headers.Location?.ToString().Should().Contain("/auth/login");

        var responseCaixa = await client.GetAsync("/caixa");
        responseCaixa.StatusCode.Should().Be(HttpStatusCode.Redirect);
        responseCaixa.Headers.Location?.ToString().Should().Contain("/auth/login");

        var responseExportacao = await client.GetAsync("/relatorios/exportacao");
        responseExportacao.StatusCode.Should().Be(HttpStatusCode.Redirect);
        responseExportacao.Headers.Location?.ToString().Should().Contain("/auth/login");
    }
}
