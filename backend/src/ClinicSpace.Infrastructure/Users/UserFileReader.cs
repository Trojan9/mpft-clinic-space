using ClinicSpace.Domain.Users;
using ClinicSpace.Infrastructure.Locations;

namespace ClinicSpace.Infrastructure.Users;

/// <summary>A user account from the supplied data file. The password is plain text in the file and is hashed on import.</summary>
public sealed record UserRow(string Name, string Email, UserRole Role, string Password);

/// <summary>
/// Reads the "Users" worksheet supplied with the exercise (columns: Name, Email Address, Role, Password),
/// or a CSV file with the same columns. "Admin" or "Administrator" maps to the Administrator role.
/// Rows with a missing value or an unknown role are reported and skipped, never guessed.
/// </summary>
public static class UserFileReader
{
    public static readonly string[] SheetNames = ["Users", "Accounts", "Staff"];

    private static readonly string[] NameHeaders = ["name", "fullname", "displayname"];
    private static readonly string[] EmailHeaders = ["emailaddress", "email", "emailaddr"];
    private static readonly string[] RoleHeaders = ["role", "userrole", "type"];
    private static readonly string[] PasswordHeaders = ["password", "initialpassword", "temporarypassword"];

    public static (IReadOnlyList<UserRow> Users, IReadOnlyList<string> Problems) Read(string path)
    {
        // In a workbook, only a sheet that is actually called "Users" (or similar) counts.
        var records = TableFileReader.Read(path, SheetNames, requirePreferredSheet: Path.GetExtension(path).Equals(".xlsx", StringComparison.OrdinalIgnoreCase));
        return Parse(records);
    }

    public static (IReadOnlyList<UserRow> Users, IReadOnlyList<string> Problems) ReadCsv(string content) =>
        Parse(TableFileReader.ParseCsv(content.TrimStart('﻿')).ToList());

    public static UserRole? ParseRole(string value) =>
        TableFileReader.NormalizeHeader(value) switch
        {
            "admin" or "administrator" => UserRole.Administrator,
            "requester" or "requestor" or "user" => UserRole.Requester,
            _ => null,
        };

    private static (IReadOnlyList<UserRow>, IReadOnlyList<string>) Parse(List<IReadOnlyList<string>> records)
    {
        var users = new List<UserRow>();
        var problems = new List<string>();
        if (records.Count == 0)
        {
            return (users, problems);
        }

        var header = records[0].Select(TableFileReader.NormalizeHeader).ToList();
        var nameIndex = TableFileReader.FindColumn(header, NameHeaders, "Name");
        var emailIndex = TableFileReader.FindColumn(header, EmailHeaders, "Email Address");
        var roleIndex = TableFileReader.FindColumn(header, RoleHeaders, "Role");
        var passwordIndex = TableFileReader.FindColumn(header, PasswordHeaders, "Password");

        for (var i = 1; i < records.Count; i++)
        {
            var record = records[i];
            string Cell(int index) => index < record.Count ? record[index].Trim() : string.Empty;
            var rowNumber = i + 1;
            var name = TableFileReader.Clean(Cell(nameIndex));
            var email = Cell(emailIndex).ToLowerInvariant();
            var roleText = Cell(roleIndex);
            var password = Cell(passwordIndex);

            if (name.Length == 0 && email.Length == 0)
            {
                continue; // Blank row.
            }

            if (name.Length == 0 || email.Length == 0 || password.Length == 0)
            {
                problems.Add($"Row {rowNumber}: name, email address and password are all required.");
                continue;
            }

            if (email.Length > User.UsernameMaxLength || !email.Contains('@', StringComparison.Ordinal))
            {
                problems.Add($"Row {rowNumber}: '{email}' is not a usable email address.");
                continue;
            }

            if (ParseRole(roleText) is not { } role)
            {
                problems.Add($"Row {rowNumber}: unknown role '{roleText}' (expected Admin or Requester).");
                continue;
            }

            users.Add(new UserRow(name, email, role, password));
        }

        return (users.DistinctBy(u => u.Email).ToList(), problems);
    }
}
