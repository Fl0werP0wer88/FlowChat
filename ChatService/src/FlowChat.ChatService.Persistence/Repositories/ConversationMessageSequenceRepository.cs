using System.Data;
using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class ConversationMessageSequenceRepository(AppDbContext dbContext)
    : IConversationMessageSequenceRepository
{
    public Task AddAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return dbContext.ConversationMessageSequences
            .AddAsync(
                new ConversationMessageSequenceEntity { ConversationId = Id<Conversation>.FromGuid(conversationId) },
                cancellationToken)
            .AsTask();
    }

    public async Task<long> GetNextAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var transaction = dbContext.Database.CurrentTransaction
            ?? throw new InvalidOperationException("A database transaction is required to allocate a conversation message sequence number.");
        var connection = dbContext.Database.GetDbConnection();

        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText =
            """
            UPDATE "ConversationMessageSequences"
            SET "LastAssignedSequenceNum" = "LastAssignedSequenceNum" + 1
            WHERE "ConversationId" = @conversationId
            RETURNING "LastAssignedSequenceNum";
            """;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@conversationId";
        parameter.DbType = DbType.Guid;
        parameter.Value = conversationId;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull)
            throw new InvalidOperationException($"Conversation message sequence for conversation {conversationId} was not found.");

        return Convert.ToInt64(result);
    }
}
