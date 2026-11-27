namespace ClinicSpace.Domain.Users;

/// <summary>
/// Application roles. Each user has exactly one role in this exercise.
/// In production these would map to Microsoft Entra ID app roles.
/// </summary>
public enum UserRole
{
    Requester = 1,
    Administrator = 2,
}
