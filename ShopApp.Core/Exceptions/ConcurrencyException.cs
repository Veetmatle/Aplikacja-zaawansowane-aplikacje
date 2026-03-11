namespace ShopApp.Core.Exceptions;

/// <summary>
/// Thrown when an optimistic concurrency conflict is detected.
/// Infrastructure layer catches DbUpdateConcurrencyException and wraps it in this type
/// so Application layer can handle retries without depending on EF Core.
/// </summary>
public class ConcurrencyException : Exception
{
    public ConcurrencyException()
        : base("A concurrency conflict occurred. The record was modified by another process.") { }

    public ConcurrencyException(string message) : base(message) { }

    public ConcurrencyException(string message, Exception innerException)
        : base(message, innerException) { }
}
