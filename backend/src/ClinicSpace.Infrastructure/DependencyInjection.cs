using ClinicSpace.Application.Abstractions;
using ClinicSpace.Infrastructure.Email;
using ClinicSpace.Infrastructure.Locations;
using ClinicSpace.Infrastructure.Persistence;
using ClinicSpace.Infrastructure.Seeding;
using ClinicSpace.Infrastructure.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ClinicSpace.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ClinicSpace")
            ?? throw new InvalidOperationException("Connection string 'ClinicSpace' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(maxRetryCount: 3);
                sql.MigrationsHistoryTable("__EFMigrationsHistory");
            }));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddOptions<EmailOptions>().Bind(configuration.GetSection(EmailOptions.SectionName)).ValidateOnStart();
        services.AddOptions<SeedOptions>().Bind(configuration.GetSection(SeedOptions.SectionName));

        services.AddScoped<IEmailTransport>(sp => sp.GetRequiredService<IOptions<EmailOptions>>().Value.Mode switch
        {
            EmailDeliveryMode.Smtp => ActivatorUtilities.CreateInstance<SmtpEmailTransport>(sp),
            EmailDeliveryMode.File => ActivatorUtilities.CreateInstance<FileEmailTransport>(sp),
            _ => ActivatorUtilities.CreateInstance<LogEmailTransport>(sp),
        });
        services.AddSingleton<EmailOutboxDispatcher>();
        services.AddHostedService(sp => sp.GetRequiredService<EmailOutboxDispatcher>());

        services.AddScoped<LocationImporter>();
        services.AddScoped<UserImporter>();
        services.AddScoped<DatabaseInitializer>();
        return services;
    }
}
