using ClinicSpace.Application.Auth;
using ClinicSpace.Application.Locations;
using ClinicSpace.Application.Notifications;
using ClinicSpace.Application.Requests;
using ClinicSpace.Domain.Users;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicSpace.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AuthOptions>().Bind(configuration.GetSection(AuthOptions.SectionName));
        services.AddOptions<RequestOptions>().Bind(configuration.GetSection(RequestOptions.SectionName));
        services.AddOptions<NotificationOptions>().Bind(configuration.GetSection(NotificationOptions.SectionName));

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

        services.AddScoped<IValidator<CreateRequestCommand>, CreateRequestValidator>();
        services.AddScoped<IValidator<DecideRequestCommand>, DecideRequestValidator>();

        services.AddScoped<AuthService>();
        services.AddScoped<LocationService>();
        services.AddScoped<RequestService>();
        services.AddScoped<RequestEmailFactory>();
        return services;
    }
}
