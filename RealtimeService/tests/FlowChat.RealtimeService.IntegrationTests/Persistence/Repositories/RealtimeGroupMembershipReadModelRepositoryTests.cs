using AutoFixture;
using FlowChat.RealtimeService.Domain.Enums;
using FlowChat.RealtimeService.Persistence;
using FlowChat.RealtimeService.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.RealtimeService.IntegrationTests.Persistence.Repositories;

public sealed class RealtimeGroupMembershipReadModelRepositoryTests : IDisposable
{
    private readonly IFixture _fixture = new Fixture();
    private readonly AppDbContext _dbContext;
    private readonly RealtimeGroupMembershipReadModelRepository _repository;

    public RealtimeGroupMembershipReadModelRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_fixture.Create<Guid>().ToString("N"))
            .Options;

        _dbContext = new AppDbContext(options);
        _repository = new RealtimeGroupMembershipReadModelRepository(_dbContext);
    }

    public void Dispose() => _dbContext.Dispose();

    [Fact]
    public async Task AddAsync_WithNewMembership_PersistsToDatabase()
    {
        var userId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        await _repository.AddAsync(userId, RealtimeGroupType.Conversation, conversationId);

        var memberships = await _repository.GetByUserIdAsync(userId);
        memberships.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { UserId = userId, GroupType = RealtimeGroupType.Conversation, ResourceId = conversationId });
    }

    [Fact]
    public async Task AddAsync_WhenMembershipAlreadyExists_DoesNotThrowOrDuplicate()
    {
        var userId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        await _repository.AddAsync(userId, RealtimeGroupType.Conversation, conversationId);
        var act = () => _repository.AddAsync(userId, RealtimeGroupType.Conversation, conversationId);

        await act.Should().NotThrowAsync();
        var memberships = await _repository.GetByUserIdAsync(userId);
        memberships.Should().ContainSingle();
    }

    [Fact]
    public async Task RemoveAsync_WithExistingMembership_DeletesIt()
    {
        var userId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        await _repository.AddAsync(userId, RealtimeGroupType.Conversation, conversationId);

        await _repository.RemoveAsync(userId, RealtimeGroupType.Conversation, conversationId);

        var memberships = await _repository.GetByUserIdAsync(userId);
        memberships.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveAsync_WhenMembershipDoesNotExist_DoesNotThrow()
    {
        var act = () => _repository.RemoveAsync(Guid.NewGuid(), RealtimeGroupType.Conversation, Guid.NewGuid());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsOnlyMembershipsForThatUser()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        await _repository.AddAsync(userId, RealtimeGroupType.Conversation, Guid.NewGuid());
        await _repository.AddAsync(otherUserId, RealtimeGroupType.Conversation, Guid.NewGuid());

        var memberships = await _repository.GetByUserIdAsync(userId);

        memberships.Should().ContainSingle().Which.UserId.Should().Be(userId);
    }
}
