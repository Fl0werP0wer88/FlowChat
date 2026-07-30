using FluentAssertions;
using FlowChat.Core.Domain;
using FlowChat.GatewayService.Api.Features.Contact.Services;
using FlowChat.GatewayService.Infrastructure.Clients.ChatService;
using FlowChat.GatewayService.Infrastructure.Clients.PresenceService;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.GatewayService.UnitTests.Features.Contact.Services;

public sealed class ContactsFacadeTests
{
    private readonly Mock<IChatServiceClient> _chatClient = new();
    private readonly Mock<IPresenceServiceClient> _presenceClient = new();

    [Fact]
    public async Task GetContactsWithConversationsAsync_ContactAndPresence_MapsAggregateFields()
    {
        var partnerId = Guid.NewGuid();
        var changedAt = DateTimeOffset.UtcNow;
        _chatClient
            .Setup(x => x.GetContactsForUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([Contact(partnerId, current: 9, read: 4)]);
        _presenceClient
            .Setup(x => x.GetPresenceStatusesAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(partnerId)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, ContactPresenceStatusClientDto>
            {
                [partnerId] = new(partnerId, PresenceStatus.Active, changedAt)
            });

        var result = await Facade().GetContactsWithConversationsAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var contact = result.Value.Contacts.Should().ContainSingle().Which;
        contact.UnreadCount.Should().Be(5);
        contact.Status.Should().Be(PresenceStatus.Active);
        contact.PresenceChangedAtUtc.Should().Be(changedAt);
    }

    [Fact]
    public async Task GetContactsWithConversationsAsync_PresenceFailure_UsesInvisibleFallback()
    {
        var partnerId = Guid.NewGuid();
        _chatClient
            .Setup(x => x.GetContactsForUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([Contact(partnerId, current: 2, read: 5)]);
        _presenceClient
            .Setup(x => x.GetPresenceStatusesAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("unavailable"));

        var result = await Facade().GetContactsWithConversationsAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var contact = result.Value.Contacts.Should().ContainSingle().Which;
        contact.UnreadCount.Should().Be(0);
        contact.Status.Should().Be(PresenceStatus.Invisible);
        contact.PresenceChangedAtUtc.Should().Be(DateTimeOffset.MinValue);
    }

    private ContactsFacade Facade() =>
        new(
            _chatClient.Object,
            _presenceClient.Object,
            NullLogger<ContactsFacade>.Instance);

    private static ContactClientDto Contact(Guid partnerId, long current, long read) =>
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
