using FlowChat.Application.Abstractions;
using FlowChat.Domain.Abstractions;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.SocialGraphs.Commands.CreateDefaultSocialGraph;
using FlowChat.SocialGraphService.Domain.Entities;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class CreateDefaultSocialGraphCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenSocialGraphDoesNotExist_CreatesItAndReturnsSuccess()
    {
        var repository = new FakeUserSocialGraphRepository();
        var unitOfWork = new FakeUnitOfWork();
        var dispatcher = new FakeDomainEventDispatcher();
        var handler = new CreateDefaultSocialGraphCommandHandler(dispatcher, unitOfWork, repository);
        var userId = Guid.NewGuid();

        var result = await handler.Handle(
            new CreateDefaultSocialGraphCommand(
                userId,
                "john.smith",
                "John",
                "Smith",
                "john.smith@flowchat.local"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedSocialGraph);
        Assert.Equal(userId, result.Value.UserId);
        Assert.Equal("john.smith", result.Value.Login);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Empty(dispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task Handle_WhenSocialGraphAlreadyExists_ReturnsConflict()
    {
        var existingUserId = Guid.NewGuid();
        var repository = new FakeUserSocialGraphRepository
        {
            ExistingSocialGraph = UserSocialGraph.Create("existing-login", existingUserId)
        };
        var unitOfWork = new FakeUnitOfWork();
        var dispatcher = new FakeDomainEventDispatcher();
        var handler = new CreateDefaultSocialGraphCommandHandler(dispatcher, unitOfWork, repository);

        var result = await handler.Handle(
            new CreateDefaultSocialGraphCommand(existingUserId, "existing-login"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict", result.Error.ErrorType.Name);
        Assert.Null(repository.AddedSocialGraph);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
        Assert.Empty(dispatcher.DispatchedEvents);
    }

    private sealed class FakeUserSocialGraphRepository : IUserSocialGraphRepository
    {
        public UserSocialGraph? ExistingSocialGraph { get; init; }
        public UserSocialGraph? AddedSocialGraph { get; private set; }

        public Task<UserSocialGraph?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                ExistingSocialGraph is not null && ExistingSocialGraph.UserId == userId
                    ? ExistingSocialGraph
                    : null);
        }

        public Task<UserSocialGraph> AddAsync(UserSocialGraph entity, CancellationToken cancellationToken = default)
        {
            AddedSocialGraph = entity;
            return Task.FromResult(entity);
        }

        public Task<Contact> AddContactAsync(Contact entity, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateContactAsync(Contact entity, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteContactAsync(Contact entity, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Invitation> AddInvitationAsync(Guid userSocialGraphId, Invitation entity, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateInvitationAsync(Invitation entity, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteInvitationAsync(Invitation entity, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public void Dispose()
        {
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken)
        {
            return operation(cancellationToken);
        }
    }

    private sealed class FakeDomainEventDispatcher : IDomainEventDispatcher
    {
        public List<IDomainEvent> DispatchedEvents { get; } = [];

        public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
        {
            DispatchedEvents.AddRange(domainEvents);
            return Task.CompletedTask;
        }
    }
}
