using CashFlow.Domain.Enums;

namespace CashFlow.Domain.Entities;

/// <summary>
/// Representa um lançamento financeiro de crédito ou débito.
/// </summary>
public class Entry
{
    public Guid Id { get; private set; }
    public EntryType Tipo { get; private set; }
    public decimal Valor { get; private set; }
    public string Descricao { get; private set; }
    public DateTimeOffset DataOcorrencia { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }

    public Entry(EntryType tipo, decimal valor, string descricao, DateTimeOffset dataOcorrencia)
    {
        // Garante que apenas tipos definidos no domínio sejam aceitos.
        if (!Enum.IsDefined(tipo))
            throw new ArgumentOutOfRangeException(nameof(tipo), "O tipo do lançamento é inválido.");

        // O tipo define crédito ou débito, portanto o valor é sempre positivo.
        if (valor <= 0)
            throw new ArgumentOutOfRangeException(nameof(valor), "O valor do lançamento deve ser maior que zero.");

        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("A descrição do lançamento é obrigatória.", nameof(descricao));

        Id = Guid.NewGuid();
        Tipo = tipo;
        Valor = valor;
        Descricao = descricao.Trim();
        DataOcorrencia = dataOcorrencia;

        // Timestamp técnico mantido em UTC.
        CriadoEm = DateTimeOffset.UtcNow;
    }
}