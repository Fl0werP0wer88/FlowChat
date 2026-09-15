using FluentAssertions;
using FlowChat.Core.Domain;
using FlowChat.GatewayService.Api.Features.Conversation.Services;
using FlowChat.GatewayService.Infrastructure.Clients.ChatService;
using FlowChat.GatewayService.Infrastructure.Clients.PresenceService;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.GatewayService.UnitTests.Features.Conversation.Services;

public sealed class DuetConversationsFacadeTests
{
    private readonly Mock<IChatServiceClient> _chatClient = new();
    private readonly Mock<IPresenceServiceClient> _presenceClient = new();

    [Fact]
    public async Task GetDuetConversationsWithPresenceAsync_ConversationAndPresence_MapsAggregateFields()
    {
        var partnerId = Guid.NewGuid();
        var changedAt = DateTimeOffset.UtcNow;
        _chatClient
            .Setup(x => x.GetDuetConversationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([DuetConversation(partnerId, current: 9, read: 4)]);
        _presenceClient
            .Setup(x => x.GetPresenceStatusesAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(partnerId)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, ContactPresenceStatusClientDto>
            {
                [partnerId] = new(partnerId, PresenceStatus.Active, changedAt)
            });

        var result = await Facade().GetDuetConversationsWithPresenceAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var conversation = result.Value.Conversations.Should().ContainSingle().Which;
        conversation.PartnerUserId.Should().Be(partnerId);
        conversation.UnreadCount.Should().Be(5);
        conversation.Status.Should().Be(PresenceStatus.Active);
        conversation.PresenceChangedAtUtc.Should().Be(changedAt);
    }

    [Fact]
    public async Task GetDuetConversationsWithPresenceAsync_PresenceFailure_UsesInvisibleFallback()
    {
        var partnerId = Guid.NewGuid();
        _chatClient
            .Setup(x => x.GetDuetConversationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([DuetConversation(partnerId, current: 2, read: 5)]);
        _presenceClient
            .Setup(x => x.GetPresenceStatusesAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("unavailable"));

        var result = await Facade().GetDuetConversationsWithPresenceAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var conversation = result.Value.Conversations.Should().ContainSingle().Which;
        conversation.UnreadCount.Should().Be(0);
        conversation.Status.Should().Be(PresenceStatus.Invisible);
        conversation.PresenceChangedAtUtc.Should().Be(DateTimeOffset.MinValue);
    }

    private DuetConversationsFacade Facade() =>
        new(
            _chatClient.Object,
            _presenceClient.Object,
            NullLogger<DuetConversationsFacade>.Instance);

    private static DuetConversationListItemClientDto DuetConversation(
        Guid partnerId,
        long current,
        long read) =>
        new(
            partnerId,
            "name",
            null,
            null,
            Guid.NewGuid(),
            read,
            current,
            false,
            false,
            false,
            false);
}
