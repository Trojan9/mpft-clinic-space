namespace ClinicSpace.Application.Common;

/// <summary>The resource does not exist, or the caller is not allowed to know it exists (HTTP 404).</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string resource)
        : base($"{resource} was not found.")
    {
    }

    public NotFoundException()
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>Someone else changed the record since the caller loaded it (HTTP 409).</summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException()
        : base("This request was changed by someone else. Reload it to see the latest version.")
    {
    }

    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }

    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
