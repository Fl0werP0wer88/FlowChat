using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class ConversationWriteRepository(AppDbContext dbContext)
    : WriteRepositoryBase<Conversation>(dbContext), IConversationWriteRepository
{
    public override async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<Conversation>()
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x => x.Id.Value == id, cancellationToken);
    }
}
