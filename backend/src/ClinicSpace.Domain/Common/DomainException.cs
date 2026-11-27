namespace ClinicSpace.Domain.Common;

/// <summary>
/// Raised when an operation would break a business rule, for example deciding a request
/// that has already been decided. The API maps this to HTTP 409 Conflict.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public DomainException()
    {
        Code = "domain_error";
    }

    public DomainException(string message)
        : base(message)
    {
        Code = "domain_error";
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
        Code = "domain_error";
    }

    /// <summary>Stable, machine-readable error code returned to API clients.</summary>
    public string Code { get; }
}
