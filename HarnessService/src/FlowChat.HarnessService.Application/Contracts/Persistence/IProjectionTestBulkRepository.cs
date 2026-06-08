using FlowChat.HarnessService.Application.Features.Projections.Commands.BulkUpsert;
using FlowChat.Shared.Application;

namespace FlowChat.HarnessService.Application.Contracts.Persistence;

public interface IProjectionTestBulkRepository : IProjectionBulkRepository<ProjectionCommandItem>
{
}
