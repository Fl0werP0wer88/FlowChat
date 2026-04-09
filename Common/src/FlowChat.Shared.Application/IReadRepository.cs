using FlowChat.Core.Contracts;

namespace FlowChat.Shared.Application;

public interface IReadRepository<TDto> where TDto : class, IDbResponse
{
    Task<TDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TDto>> GetAllAsync(CancellationToken cancellationToken = default);
}

