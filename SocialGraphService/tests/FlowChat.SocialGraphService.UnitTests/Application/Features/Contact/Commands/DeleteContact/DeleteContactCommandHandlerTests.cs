using AutoFixture;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.Contact.Commands.DeleteContact;
using FlowChat.SocialGraphService.Domain.Entities.Contact;
using FluentAssertions;
using MediatR;
using Moq;
using UserProfileMarker = FlowChat.SocialGraphService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class DeleteContactCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IContactWriteRepository> _contactWriteRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILocalEventDispatcher> _domainEventDispatcherMock = new();
    private readonly DeleteContactCommandHandler _handler;

    public DeleteContactCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new DeleteContactCommandHandler(
            _contactWriteRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object,
            []);
    }

    [Fact]
    public async Task Handle_WhenContactExists_DeletesContactAndReturnsSuccess()
    {
        var contact = Contact.Rehydrate(Id<UserProfileMarker>.New(), Id<UserProfileMarker>.New(), "John Doe");
        _contactWriteRepositoryMock
            .Setup(x => x.GetByOwnerAndContactAsync(contact.OwnerUserId, contact.ContactUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contact);

        var result = await _handler.Handle(
            new DeleteContactCommand(contact.OwnerUserId.Value, contact.ContactUserId.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Unit.Value);
        _contactWriteRepositoryMock.Verify(
            x => x.DeleteAsync(contact, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenContactDoesNotExist_ReturnsNotFound()
    {
        var ownerUserId = _fixture.Create<Guid>();
        var contactUserId = _fixture.Create<Guid>();

        _contactWriteRepositoryMock
            .Setup(x => x.GetByOwnerAndContactAsync(
                Id<UserProfileMarker>.FromGuid(ownerUserId),
                Id<UserProfileMarker>.FromGuid(contactUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact?)null);

        var result = await _handler.Handle(
            new DeleteContactCommand(ownerUserId, contactUserId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
        _contactWriteRepositoryMock.Verify(
            x => x.DeleteAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

