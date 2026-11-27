using ClinicSpace.Domain.Users;

namespace ClinicSpace.Application.Auth;

public enum SignInFailure
{
    None = 0,
    InvalidCredentials = 1,
    LockedOut = 2,
}

public sealed record SignInResult(User? User, SignInFailure Failure)
{
    public bool Succeeded => User is not null && Failure == SignInFailure.None;

    public static SignInResult Success(User user) => new(user, SignInFailure.None);

    public static SignInResult Failed(SignInFailure failure) => new(null, failure);
}
