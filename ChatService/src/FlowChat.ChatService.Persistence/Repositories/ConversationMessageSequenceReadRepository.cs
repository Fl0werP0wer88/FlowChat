using FlowChat.ChatService.Application.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class ConversationMessageSequenceReadRepository(AppDbContext dbContext)
    : IConversationMessageSequenceReadRepository
{
    public Task<long?> GetCurrentAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.ConversationMessageSequencesV2
            .AsNoTracking()
            .Where(sequence => sequence.ConversationId.Value == conversationId)
            .Select(sequence => (long?)sequence.LastAssignedSequenceNum)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
