using Menova.Domain.Menu;
using Menova.Domain.Sala;
using Xunit;

namespace Menova.UnitTests;

public class MenuAndSalaTests
{
    [Fact]
    public void Artigo_QuandoEsgotadoManual_NaoDeveEstarDisponivelParaVenda()
    {
        // Arrange
        var artigo = new Artigo
        {
            Nome = "Frango à Zambeziana",
            CodigoInterno = "PRT-001",
            Preco = 650.00m,
            TaxaIva = 16.00m,
            Ativo = true,
            Disponivel = true,
            EsgotadoManual = false,
            EstaEliminado = false
        };

        // Act & Assert 1: Deve estar disponível
        Assert.True(artigo.EstaDisponivelParaVenda);

        // Act 2: Ativa regra 86 (esgotado manual)
        artigo.EsgotadoManual = true;

        // Assert 2: Deixa imediatamente de estar disponível para venda
        Assert.False(artigo.EstaDisponivelParaVenda);
    }

    [Fact]
    public void Artigo_QuandoEliminado_NaoDeveEstarDisponivelParaVenda()
    {
        // Arrange
        var artigo = new Artigo
        {
            Nome = "Cerveja Laurentina Preta",
            CodigoInterno = "BEB-001",
            Preco = 130.00m,
            Ativo = true,
            Disponivel = true,
            EsgotadoManual = false,
            EstaEliminado = true
        };

        // Assert
        Assert.False(artigo.EstaDisponivelParaVenda);
    }

    [Theory]
    [InlineData(EstadoMesa.Livre)]
    [InlineData(EstadoMesa.Ocupada)]
    [InlineData(EstadoMesa.ContaPedida)]
    [InlineData(EstadoMesa.Reservada)]
    [InlineData(EstadoMesa.Bloqueada)]
    public void Mesa_DeveSuportarTodosOsEstadosDefinidos(EstadoMesa estado)
    {
        // Arrange
        var mesa = new Mesa
        {
            Numero = "01",
            Lugares = 4,
            Estado = estado
        };

        // Assert
        Assert.Equal(estado, mesa.Estado);
    }
}
