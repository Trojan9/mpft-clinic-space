using ClinicSpace.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicSpace.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", t => t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Requester', 'Administrator')"));
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Username).HasMaxLength(User.UsernameMaxLength).IsRequired();
        builder.Property(u => u.NormalizedUsername).HasMaxLength(User.UsernameMaxLength).IsRequired();
        builder.HasIndex(u => u.NormalizedUsername).IsUnique();
        builder.Property(u => u.DisplayName).HasMaxLength(User.DisplayNameMaxLength).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(User.EmailMaxLength).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Ignore(u => u.IsAdministrator);
    }
}
