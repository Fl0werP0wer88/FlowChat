using FlowChat.Core.Contracts;

namespace FlowChat.HarnessService.Application.Features.Projections;

public sealed class ProjectionTestDto : IDbReadResponse
{
    public Guid Id { get; init; }
    public string Payload { get; init; } = string.Empty;
}
