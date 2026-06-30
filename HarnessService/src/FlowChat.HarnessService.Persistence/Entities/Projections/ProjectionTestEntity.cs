using FlowChat.Shared.Persistance;

namespace FlowChat.HarnessService.Persistence.Entities.Projections;

public sealed class ProjectionTestEntity : ReadModelEntityBase
{
    public Guid Id { get; set; }
    public string Payload { get; set; } = string.Empty;
}
