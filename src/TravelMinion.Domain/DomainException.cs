namespace TravelMinion.Domain;

/// <summary>
/// Raised when an attempt is made to put the domain into a state it forbids.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }
}
