using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Persistence.ReadModels;
using FlowChat.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class DuetConversationRepository : IDuetConversationRepository
{
    private readonly AppDbContext _dbContext;

    public DuetConversationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Guid?> FindConversationIdAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken = default)
    {
        var (first, second) = Normalize(userId1, userId2);

        var entry = await _dbContext.DuetConversations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.FirstUserId == first && x.SecondUserId == second, cancellationToken);

        return entry?.ConversationId.Value;
    }

    public Task AddAsync(Guid userId1, Guid userId2, Guid conversationId, CancellationToken cancellationToken = default)
    {
        var (first, second) = Normalize(userId1, userId2);

        _dbContext.DuetConversations.Add(new DuetConversation
        {
            FirstUserId = first,
            SecondUserId = second,
            ConversationId = Id<Conversation>.FromGuid(conversationId)
        });

        return Task.CompletedTask;
    }

    private static (Guid First, Guid Second) Normalize(Guid userId1, Guid userId2) =>
        userId1 < userId2 ? (userId1, userId2) : (userId2, userId1);
}
