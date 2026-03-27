using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace FlowChat.Shared.Application.Observability;

public static class ApplicationActivitySource
{
    private static readonly ConcurrentDictionary<string, ActivitySource> Sources = new(StringComparer.Ordinal);

    public static ActivitySource For<TRequest>() => For(typeof(TRequest).Assembly);

    public static ActivitySource For(Assembly assembly)
    {
        var sourceName = assembly.GetName().Name ?? "FlowChat.Application";

        return Sources.GetOrAdd(sourceName, static name => new ActivitySource(name));
    }
}

