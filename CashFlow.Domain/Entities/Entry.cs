using CashFlow.Domain.Enums;

namespace CashFlow.Domain.Entities;

/// <summary>
/// Representa um lançamento financeiro de crédito ou débito.
/// </summary>
public class Entry
{
    // Identificador único do lançamento.
    public Guid Id { get; private set; }

    // Define se o lançamento representa uma entrada ou saída financeira.
    public EntryType Tipo { get; private set; }

    // Valor financeiro do lançamento, sempre armazenado como positivo.
    public decimal Valor { get; private set; }

    // Descrição informada para identificar o lançamento.
    public string Descricao { get; private set; }

    // Data e hora em que a movimentação financeira ocorreu.
    public DateTimeOffset DataOcorrencia { get; private set; }

    // Data e hora em que o lançamento foi criado no sistema.
    public DateTimeOffset CriadoEm { get; private set; }

    public Entry(EntryType tipo, decimal valor, string descricao, DateTimeOffset dataOcorrencia)
    {
        // Garante que apenas tipos definidos no domínio sejam aceitos.
        if (!Enum.IsDefined(tipo))
            throw new ArgumentOutOfRangeException(nameof(tipo), "O tipo do lançamento é inválido.");

        // Crédito ou débito é definido pelo tipo, portanto o valor é sempre positivo.
        if (valor <= 0)
            throw new ArgumentOutOfRangeException(nameof(valor), "O valor do lançamento deve ser maior que zero.");

        // Evita lançamentos sem uma identificação mínima.
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("A descrição do lançamento é obrigatória.", nameof(descricao));

        Id = Guid.NewGuid();
        Tipo = tipo;
        Valor = valor;
        Descricao = descricao.Trim();
        DataOcorrencia = dataOcorrencia;
        CriadoEm = DateTimeOffset.UtcNow;
    }
}