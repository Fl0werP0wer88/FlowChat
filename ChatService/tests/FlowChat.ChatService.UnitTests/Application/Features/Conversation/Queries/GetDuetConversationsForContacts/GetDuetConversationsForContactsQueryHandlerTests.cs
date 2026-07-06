using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.ChatService.Application.Features.Conversation.Queries.GetDuetConversationsForContacts;
using FluentAssertions;
using Moq;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Queries.GetDuetConversationsForContacts;

public sealed class GetDuetConversationsForContactsQueryHandlerTests
{
    private readonly Mock<IDuetConversationReadRepository> _repositoryMock = new();
    private readonly GetDuetConversationsForContactsQueryHandler _handler;

    public GetDuetConversationsForContactsQueryHandlerTests()
    {
        _handler = new GetDuetConversationsForContactsQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenConversationsExist_ReturnsSuccess()
    {
        var requestingUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var query = new GetDuetConversationsForContactsQuery(requestingUserId, [partnerUserId]);
        IReadOnlyCollection<DuetConversationForContactDto> dtos =
        [
            new DuetConversationForContactDto(partnerUserId, Guid.NewGuid(), 42, 84)
        ];

        _repositoryMock
            .Setup(x => x.GetConversationsForContactsAsync(
                requestingUserId,
                query.PartnerUserIds,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(dtos);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(dtos);
    }
}
