using FlowChat.ChatService.Persistence.Configuration.Entities;
using FlowChat.Shared.Application.Contracts.Persistence;
using FlowChat.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class ConversationDbUpdateExceptionMapper : IDbUpdateExceptionMapper
{
    public IDomainError? Map(DbUpdateException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: ConversationV2Configuration.DuetParticipantPairUniqueIndexName
        }
            ? DomainError.Conflict("Duet conversation already exists.")
            : null;
    }
}
