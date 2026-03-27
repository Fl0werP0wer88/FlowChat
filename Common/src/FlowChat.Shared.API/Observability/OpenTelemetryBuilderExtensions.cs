using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FlowChat.Shared.API;

public static class OpenTelemetryBuilderExtensions
{
    private static readonly string[] BuiltInActivitySourceNames =
    [
        "Silverback.Integration.Produce",
        "Silverback.Integration.Consume"
    ];

    public static WebApplicationBuilder AddFlowChatOpenTelemetry(
        this WebApplicationBuilder builder,
        params Assembly[] activitySourceAssemblies)
    {
        ConfigureOpenTelemetry(
            builder.Services,
            builder.Logging,
            builder.Configuration,
            builder.Environment,
            includeAspNetCoreInstrumentation: true,
            activitySourceAssemblies);

        return builder;
    }

    public static HostApplicationBuilder AddFlowChatOpenTelemetry(
        this HostApplicationBuilder builder,
        params Assembly[] activitySourceAssemblies)
    {
        ConfigureOpenTelemetry(
            builder.Services,
            builder.Logging,
            builder.Configuration,
            builder.Environment,
            includeAspNetCoreInstrumentation: false,
            activitySourceAssemblies);

        return builder;
    }

    private static void ConfigureOpenTelemetry(
        IServiceCollection services,
        ILoggingBuilder logging,
        IConfiguration configuration,
        IHostEnvironment environment,
        bool includeAspNetCoreInstrumentation,
        params Assembly[] activitySourceAssemblies)
    {
        var serviceName = ResolveOpenTelemetryServiceName(configuration, environment);
        var serviceVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
        var environmentName = environment.EnvironmentName;
        var otlpEndpoint = GetOtlpEndpoint(configuration);
        var activitySourceNames = ResolveActivitySourceNames(activitySourceAssemblies);

        logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.ParseStateValues = true;
            options.SetResourceBuilder(CreateOpenTelemetryResourceBuilder(serviceName, serviceVersion, environmentName));

            if (string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                return;
            }

            options.AddOtlpExporter(exporterOptions => ConfigureOtlpExporter(exporterOptions, configuration, otlpEndpoint));
        });

        services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(serviceName: serviceName, serviceVersion: serviceVersion);
                resource.AddAttributes(
                [
                    new KeyValuePair<string, object>("deployment.environment", environmentName)
                ]);
            })
            .WithTracing(tracing =>
            {
                foreach (var activitySourceName in activitySourceNames)
                {
                    tracing.AddSource(activitySourceName);
                }

                if (includeAspNetCoreInstrumentation)
                {
                    tracing.AddAspNetCoreInstrumentation(options => options.RecordException = true);
                }

                tracing.AddHttpClientInstrumentation();

                if (string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    return;
                }

                tracing.AddOtlpExporter(exporterOptions => ConfigureOtlpExporter(exporterOptions, configuration, otlpEndpoint));
            });
    }

    private static string ResolveOpenTelemetryServiceName(IConfiguration configuration, IHostEnvironment environment)
    {
        var serviceName = configuration["OpenTelemetry:ServiceName"];
        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            return serviceName;
        }

        return environment.ApplicationName;
    }

    private static string? GetOtlpEndpoint(IConfiguration configuration) =>
        configuration["OpenTelemetry:Otlp:Endpoint"];

    private static string[] ResolveActivitySourceNames(Assembly[] assemblies) =>
        BuiltInActivitySourceNames
            .Concat(
                assemblies.Select(assembly => assembly.GetName().Name))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .Cast<string>()
            .ToArray();

    private static ResourceBuilder CreateOpenTelemetryResourceBuilder(
        string serviceName,
        string? serviceVersion,
        string environmentName) =>
        ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
            .AddAttributes(
            [
                new KeyValuePair<string, object>("deployment.environment", environmentName)
            ]);

    private static void ConfigureOtlpExporter(
        OtlpExporterOptions options,
        IConfiguration configuration,
        string endpoint)
    {
        options.Endpoint = new Uri(endpoint, UriKind.Absolute);

        var protocol = configuration["OpenTelemetry:Otlp:Protocol"];
        if (Enum.TryParse<OtlpExportProtocol>(protocol, ignoreCase: true, out var parsedProtocol))
        {
            options.Protocol = parsedProtocol;
        }

        var headers = configuration["OpenTelemetry:Otlp:Headers"];
        if (!string.IsNullOrWhiteSpace(headers))
        {
            options.Headers = headers;
        }
    }
}

