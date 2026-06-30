namespace FlowChat.HarnessService.Consumers.Projections.Models;

public sealed record ProjectionTestReadModel
{
    public required string Payload { get; init; }
}
