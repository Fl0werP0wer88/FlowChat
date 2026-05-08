using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.ChatService.Application.Features.Conversation.Queries.GetDuetConversation;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Moq;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Queries.GetDuetConversation;

public sealed class GetDuetConversationQueryHandlerTests
{
    private readonly Mock<IDuetConversationReadRepository> _repositoryMock = new();
    private readonly GetDuetConversationQueryHandler _handler;

    public GetDuetConversationQueryHandlerTests()
    {
        _handler = new GetDuetConversationQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenConversationExists_ReturnsSuccess()
    {
        var requestingUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var query = new GetDuetConversationQuery(requestingUserId, partnerUserId);
        var dto = new DuetConversationDetailDto(
            Guid.NewGuid(),
            [
                new ConversationParticipantDto(requestingUserId, "Requester", "req.png", requestingUserId),
                new ConversationParticipantDto(partnerUserId, "Partner", "partner.png", partnerUserId)
            ]);

        _repositoryMock
            .Setup(x => x.GetByUserIdsAsync(requestingUserId, partnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(dto);
    }

    [Fact]
    public async Task Handle_WhenConversationNotFound_ReturnsNotFoundError()
    {
        var query = new GetDuetConversationQuery(Guid.NewGuid(), Guid.NewGuid());

        _repositoryMock
            .Setup(x => x.GetByUserIdsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DuetConversationDetailDto?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }
}
