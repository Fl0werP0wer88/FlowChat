using FlowChat.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FlowChat.Shared.Infrastructure.Http;

public static class HttpClientServiceCollectionExtensions
{
    public static IHttpClientBuilder AddFlowChatHttpClient<TInterface, TImpl>(
        this IServiceCollection services,
        Action<IServiceProvider, HttpClient> configure)
        where TInterface : class
        where TImpl : FlowChatHttpClientBase, TInterface
        => services.AddHttpClient<TInterface, TImpl>(configure);

    public static IHttpClientBuilder AddFlowChatHttpClient<TInterface, TImpl, TSettings>(
        this IServiceCollection services)
        where TInterface : class
        where TImpl : FlowChatHttpClientBase, TInterface
        where TSettings : class, IInternalApiSettingSection, new()
    {
        services.AddOptions<TSettings>()
            .BindConfiguration(new TSettings().SectionName);

        return services.AddFlowChatHttpClient<TInterface, TImpl>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<TSettings>>().Value;
            if (!Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var baseAddress))
                throw new InvalidOperationException($"{settings.SectionName}:BaseUrl must be an absolute URI.");

            client.BaseAddress = baseAddress;
            client.DefaultRequestHeaders.Remove(FlowChatHttpClientBase.InternalApiKeyHeaderName);
            if (!string.IsNullOrWhiteSpace(settings.ApiKey))
                client.DefaultRequestHeaders.Add(FlowChatHttpClientBase.InternalApiKeyHeaderName, settings.ApiKey);
        });
    }
}
