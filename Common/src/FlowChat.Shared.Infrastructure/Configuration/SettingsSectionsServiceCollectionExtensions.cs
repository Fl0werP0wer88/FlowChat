using System.Linq.Expressions;
using System.Reflection;
using FlowChat.Core.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FlowChat.Shared.Infrastructure.Configuration;

public static class SettingsSectionsServiceCollectionExtensions
{
    private static readonly MethodInfo _bindMethod = FindBindMethod();

    public static IServiceCollection AddSettingsSections(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] assemblies)
    {
        services.AddOptions();

        var settingsTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && t.IsAssignableTo(typeof(SettingsSectionBase)));

        foreach (var settingsType in settingsTypes)
        {
            var instance = (SettingsSectionBase)Activator.CreateInstance(settingsType)!;
            var section = configuration.GetSection(instance.SectionName);
            RegisterSection(services, settingsType, section);
        }

        return services;
    }

    private static void RegisterSection(IServiceCollection services, Type settingsType, IConfiguration section)
    {
        var param = Expression.Parameter(settingsType, "opts");
        var body = Expression.Call(
            _bindMethod,
            Expression.Constant(section, typeof(IConfiguration)),
            Expression.Convert(param, typeof(object)));
        var actionType = typeof(Action<>).MakeGenericType(settingsType);
        var action = Expression.Lambda(actionType, body, param).Compile();

        var configureNamedOptionsType = typeof(ConfigureNamedOptions<>).MakeGenericType(settingsType);
        var configureOptions = Activator.CreateInstance(configureNamedOptionsType, [string.Empty, action])!;
        services.AddSingleton(typeof(IConfigureOptions<>).MakeGenericType(settingsType), configureOptions);
    }

    private static MethodInfo FindBindMethod() =>
        typeof(ConfigurationBinder)
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .First(m =>
                m.Name == "Bind"
                && !m.IsGenericMethodDefinition
                && m.GetParameters() is { Length: 2 } ps
                && ps[0].ParameterType == typeof(IConfiguration)
                && ps[1].ParameterType == typeof(object));
}
