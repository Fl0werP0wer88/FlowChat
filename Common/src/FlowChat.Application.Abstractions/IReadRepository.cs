namespace FlowChat.Application.Abstractions;

public interface IReadRepository<TDto> where TDto : class
{
    Task<TDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
