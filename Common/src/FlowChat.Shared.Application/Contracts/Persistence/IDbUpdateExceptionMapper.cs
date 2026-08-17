using FlowChat.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.Shared.Application.Contracts.Persistence;

public interface IDbUpdateExceptionMapper
{
    IDomainError? Map(DbUpdateException exception);
}
