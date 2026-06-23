using FlowChat.Core.Contracts;

namespace FlowChat.GatewayService.Api.Models;

public sealed class GatewayStatusResponse : IServiceOutput
{
    public string Service { get; init; } = string.Empty;

    public string Environment { get; init; } = string.Empty;

    public string AuthenticationMode { get; init; } = string.Empty;

    public DateTimeOffset UtcNow { get; init; }

    public string Status { get; init; } = string.Empty;
}
