using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class GroupConversationWriteRepository(AppDbContext dbContext)
    : WriteRepositoryBase<GroupConversation, Conversation>(dbContext), IGroupConversationWriteRepository
{
    // Eagerly loads participants required by all handlers that mutate group state
    public override async Task<GroupConversation?> GetByIdAsync(Id<Conversation> id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<GroupConversation>()
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}


