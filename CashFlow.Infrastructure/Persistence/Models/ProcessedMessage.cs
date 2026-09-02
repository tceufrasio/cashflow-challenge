namespace CashFlow.Infrastructure.Persistence.Models;

/// <summary>
/// Registra uma mensagem já processada pelo consolidado.
/// </summary>
public sealed class ProcessedMessage
{
    public Guid EntryId { get; private set; }
    public DateTimeOffset ProcessedAt { get; private set; }

    private ProcessedMessage() { }

    public ProcessedMessage(Guid entryId)
    {
        EntryId = entryId;
        ProcessedAt = DateTimeOffset.UtcNow;
    }
}