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
    public async Task AddRangeAsync_WithNewMemberships_PersistsToDatabase()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        await _repository.AddRangeAsync([userId, otherUserId], RealtimeGroupType.Conversation, conversationId);

        var memberships = await _repository.GetByUserIdAsync(userId);
        memberships.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { UserId = userId, GroupType = RealtimeGroupType.Conversation, ResourceId = conversationId });
    }

    [Fact]
    public async Task AddRangeAsync_WhenMembershipAlreadyExists_DoesNotThrowOrDuplicate()
    {
        var userId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        await _repository.AddRangeAsync([userId], RealtimeGroupType.Conversation, conversationId);
        var act = () => _repository.AddRangeAsync([userId], RealtimeGroupType.Conversation, conversationId);

        await act.Should().NotThrowAsync();
        var memberships = await _repository.GetByUserIdAsync(userId);
        memberships.Should().ContainSingle();
    }

    [Fact]
    public async Task RemoveRangeAsync_WithExistingMemberships_DeletesThem()
    {
        var userId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        await _repository.AddRangeAsync([userId], RealtimeGroupType.Conversation, conversationId);

        await _repository.RemoveRangeAsync([userId], RealtimeGroupType.Conversation, conversationId);

        var memberships = await _repository.GetByUserIdAsync(userId);
        memberships.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveRangeAsync_WhenMembershipDoesNotExist_DoesNotThrow()
    {
        var act = () => _repository.RemoveRangeAsync([Guid.NewGuid()], RealtimeGroupType.Conversation, Guid.NewGuid());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsOnlyMembershipsForThatUser()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        await _repository.AddRangeAsync([userId], RealtimeGroupType.Conversation, Guid.NewGuid());
        await _repository.AddRangeAsync([otherUserId], RealtimeGroupType.Conversation, Guid.NewGuid());

        var memberships = await _repository.GetByUserIdAsync(userId);

        memberships.Should().ContainSingle().Which.UserId.Should().Be(userId);
    }
}
