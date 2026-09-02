using CashFlow.Domain.Entities;
using CashFlow.Domain.Enums;

namespace CashFlow.Domain.Tests.Entities;

public class LancamentoTests
{
    // Verifica se um lançamento com dados válidos é criado corretamente.
    [Fact]
    public void CriarLancamento_ComDadosValidos_DeveCriarLancamento()
    {
        // Preparação
        var tipo = TipoLancamento.Credito;
        var valor = 150.50m;
        var descricao = "Venda realizada";
        var dataOcorrencia = DateTimeOffset.UtcNow;

        // Execução
        var lancamento = new Lancamento(tipo, valor, descricao, dataOcorrencia);

        // Validação
        Assert.NotEqual(Guid.Empty, lancamento.Id);
        Assert.Equal(tipo, lancamento.Tipo);
        Assert.Equal(valor, lancamento.Valor);
        Assert.Equal(descricao, lancamento.Descricao);
        Assert.Equal(dataOcorrencia, lancamento.DataOcorrencia);
        Assert.NotEqual(default, lancamento.CriadoEm);
    }

    // Verifica se valores iguais ou menores que zero são rejeitados.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void CriarLancamento_ComValorInvalido_DeveLancarExcecao(decimal valor)
    {
        // Execução
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Lancamento(TipoLancamento.Credito, valor, "Venda realizada", DateTimeOffset.UtcNow));

        // Validação
        Assert.Equal("valor", exception.ParamName);
    }

    // Verifica se descrições nulas, vazias ou contendo apenas espaços são rejeitadas.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CriarLancamento_ComDescricaoInvalida_DeveLancarExcecao(string? descricao)
    {
        // Execução
        var exception = Assert.Throws<ArgumentException>(() => new Lancamento(TipoLancamento.Credito, 100m, descricao!, DateTimeOffset.UtcNow));

        // Validação
        Assert.Equal("descricao", exception.ParamName);
    }

    // Verifica se um tipo não definido no domínio é rejeitado.
    [Fact]
    public void CriarLancamento_ComTipoInvalido_DeveLancarExcecao()
    {
        // Preparação
        var tipoInvalido = (TipoLancamento)999;

        // Execução
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Lancamento(tipoInvalido, 100m, "Venda realizada", DateTimeOffset.UtcNow));

        // Validação
        Assert.Equal("tipo", exception.ParamName);
    }

    // Verifica se espaços extras no início e no final da descrição são removidos.
    [Fact]
    public void CriarLancamento_ComDescricaoComEspacos_DeveRemoverEspacos()
    {
        // Preparação
        var descricao = "  Venda realizada  ";

        // Execução
        var lancamento = new Lancamento(TipoLancamento.Credito, 100m, descricao, DateTimeOffset.UtcNow);

        // Validação
        Assert.Equal("Venda realizada", lancamento.Descricao);
    }
}