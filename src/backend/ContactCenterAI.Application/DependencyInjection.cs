using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ContactCenterAI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddAutoMapper(assembly);

        return services;
    }
}
