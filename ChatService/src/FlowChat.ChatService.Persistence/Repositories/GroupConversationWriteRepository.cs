using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class GroupConversationWriteRepository(AppDbContext dbContext) : IGroupConversationWriteRepository
{
    public async Task<GroupConversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var typedId = Id<Conversation>.FromGuid(id);

        return await dbContext.Set<GroupConversation>()
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x => x.Id == typedId, cancellationToken);
    }

    public async Task<GroupConversation> AddAsync(GroupConversation conversation, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<GroupConversation>().AddAsync(conversation, cancellationToken);
        return conversation;
    }

    public Task UpdateAsync(GroupConversation conversation, CancellationToken cancellationToken = default)
    {
        dbContext.Set<GroupConversation>().Update(conversation);
        return Task.CompletedTask;
    }
}
