using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FlowChat.Shared.Infrastructure.Http;

public static class HttpClientServiceCollectionExtensions
{
    public static IHttpClientBuilder AddFlowChatHttpClient<TInterface, TImpl>(
        this IServiceCollection services,
        Action<HttpClient> configure)
        where TInterface : class
        where TImpl : class, TInterface
        => services.AddHttpClient<TInterface, TImpl>(configure);

    public static IHttpClientBuilder AddFlowChatHttpClient<TInterface, TImpl>(
        this IServiceCollection services,
        Action<IServiceProvider, HttpClient> configure)
        where TInterface : class
        where TImpl : class, TInterface
        => services.AddHttpClient<TInterface, TImpl>(configure);
}
