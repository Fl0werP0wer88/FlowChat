using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FlowChat.Shared.Application;

public static class ServiceCollectionValidationExtensions
{
    public static IServiceCollection AddFlowChatValidatorsFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        var validatorRegistrations = assembly
            .DefinedTypes
            .Where(type => !type.IsAbstract && !type.IsInterface && !type.ContainsGenericParameters)
            .SelectMany(
                type => type.ImplementedInterfaces
                    .Where(@interface => @interface.IsGenericType
                        && @interface.GetGenericTypeDefinition() == typeof(IValidator<>))
                    .Select(@interface => new
                    {
                        ServiceType = @interface,
                        ImplementationType = type.AsType()
                    }))
            .Distinct();

        foreach (var registration in validatorRegistrations)
        {
            services.TryAddEnumerable(ServiceDescriptor.Scoped(
                registration.ServiceType,
                registration.ImplementationType));
        }

        return services;
    }
}

