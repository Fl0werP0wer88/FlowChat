using AutoFixture;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.Contacts.Queries.GetContactsForUser;
using FluentAssertions;
using Moq;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class GetContactsForUserQueryHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IContactReadRepository> _contactReadRepositoryMock = new();
    private readonly GetContactsForUserQueryHandler _handler;

    public GetContactsForUserQueryHandlerTests()
    {
        _handler = new GetContactsForUserQueryHandler(_contactReadRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenContactsExist_ReturnsSuccessWithContacts()
    {
        var userId = _fixture.Create<Guid>();
        IReadOnlyList<ContactDto> expectedContacts =
        [
            new(
                _fixture.Create<Guid>(),
                userId,
                _fixture.Create<Guid>(),
                "Jane Doe",
                "Jane",
                "Doe",
                "+48123123123",
                "jane@example.com",
                false),
            new(
                _fixture.Create<Guid>(),
                userId,
                _fixture.Create<Guid>(),
                "John Doe",
                "John",
                "Doe",
                null,
                null,
                true)
        ];

        _contactReadRepositoryMock
            .Setup(x => x.GetForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedContacts);

        var result = await _handler.Handle(new GetContactsForUserQuery(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedContacts);
        _contactReadRepositoryMock.Verify(
            x => x.GetForUserAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
