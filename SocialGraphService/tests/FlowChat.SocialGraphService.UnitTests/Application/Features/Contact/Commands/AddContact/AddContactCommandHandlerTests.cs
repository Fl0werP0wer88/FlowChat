using AutoFixture;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.Contact.Commands.AddContact;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Domain.Entities.Contact;
using FluentAssertions;
using Moq;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class AddContactCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IContactWriteRepository> _contactWriteRepositoryMock = new();
    private readonly Mock<IUserProfileProjectionReadRepository> _userProfileProjectionReadRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly AddContactCommandHandler _handler;

    public AddContactCommandHandlerTests()
    {
        _contactWriteRepositoryMock
            .Setup(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _contactWriteRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contact contact, CancellationToken _) => contact);

        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Guid>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Guid>>>, CancellationToken>((operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new AddContactCommandHandler(
            _contactWriteRepositoryMock.Object,
            _userProfileProjectionReadRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserIdMatchesProjection_AddsContactAndReturnsContactId()
    {
        Contact? capturedContact = null;
        var ownerUserId = _fixture.Create<Guid>();
        var contactUserId = _fixture.Create<Guid>();

        _userProfileProjectionReadRepositoryMock
            .Setup(x => x.GetByUserProfileIdAsync(contactUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfileProjection(
                contactUserId,
                "jdoe",
                "jane@example.com",
                "+48123123123",
                null,
                null,
                true,
                _fixture.Create<DateTimeOffset>(),
                "Jane",
                "Doe",
                "FlowChat"));

        _contactWriteRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
            .Callback<Contact, CancellationToken>((contact, _) => capturedContact = contact)
            .ReturnsAsync((Contact contact, CancellationToken _) => contact);

        var result = await _handler.Handle(
            new AddContactCommand(ownerUserId, contactUserId, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        capturedContact.Should().NotBeNull();
        capturedContact!.OwnerUserId.Should().Be(ownerUserId);
        capturedContact.ContactUserId.Should().Be(contactUserId);
        capturedContact.DisplayName.Should().Be("Jane Doe");
        capturedContact.FirstName.Should().Be("Jane");
        capturedContact.LastName.Should().Be("Doe");
        capturedContact.EmailAddress!.Value.Should().Be("jane@example.com");
        capturedContact.PhoneNumber!.Value.Should().Be("+48123123123");
    }

    [Fact]
    public async Task Handle_WhenFriendlyUserIdMatchesProjection_AddsContactUsingThatLookup()
    {
        var ownerUserId = _fixture.Create<Guid>();
        var projectionUserId = _fixture.Create<Guid>();

        _userProfileProjectionReadRepositoryMock
            .Setup(x => x.GetByFriendlyUserIdAsync("jdoe", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfileProjection(
                projectionUserId,
                "jdoe",
                null,
                "+48123123123",
                null,
                null,
                true,
                null,
                "Jane",
                "Doe",
                "FlowChat"));

        var result = await _handler.Handle(
            new AddContactCommand(ownerUserId, null, " jdoe ", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _userProfileProjectionReadRepositoryMock.Verify(
            x => x.GetByFriendlyUserIdAsync("jdoe", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailMatchesProjection_UsesNormalizedEmailLookup()
    {
        var ownerUserId = _fixture.Create<Guid>();
        var projectionUserId = _fixture.Create<Guid>();

        _userProfileProjectionReadRepositoryMock
            .Setup(x => x.GetByEmailAsync("john@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfileProjection(
                projectionUserId,
                "jdoe",
                "JOHN@example.com",
                null,
                null,
                null,
                true,
                null,
                "John",
                "Doe",
                "FlowChat"));

        var result = await _handler.Handle(
            new AddContactCommand(ownerUserId, null, null, " JOHN@example.com "),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _userProfileProjectionReadRepositoryMock.Verify(
            x => x.GetByEmailAsync("john@example.com", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProjectionDoesNotExist_ReturnsNotFound()
    {
        _userProfileProjectionReadRepositoryMock
            .Setup(x => x.GetByUserProfileIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfileProjection?)null);

        var result = await _handler.Handle(
            new AddContactCommand(_fixture.Create<Guid>(), _fixture.Create<Guid>(), null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.ErrorMessage.Should().Be("User profile projection was not found.");
        _contactWriteRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenContactAlreadyExists_ReturnsConflict()
    {
        var ownerUserId = _fixture.Create<Guid>();
        var projectionUserId = _fixture.Create<Guid>();

        _userProfileProjectionReadRepositoryMock
            .Setup(x => x.GetByUserProfileIdAsync(projectionUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfileProjection(
                projectionUserId,
                "jdoe",
                null,
                null,
                null,
                null,
                true,
                null,
                "Jane",
                "Doe",
                "FlowChat"));

        _contactWriteRepositoryMock
            .Setup(x => x.ExistsAsync(ownerUserId, projectionUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(
            new AddContactCommand(ownerUserId, projectionUserId, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
        result.Error.ErrorMessage.Should().Be("Contact already exists.");
    }

    [Fact]
    public async Task Handle_WhenProjectionBelongsToOwner_ReturnsBadRequest()
    {
        var ownerUserId = _fixture.Create<Guid>();

        _userProfileProjectionReadRepositoryMock
            .Setup(x => x.GetByUserProfileIdAsync(ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfileProjection(
                ownerUserId,
                "self",
                null,
                null,
                null,
                null,
                true,
                null,
                "Self",
                "User",
                "FlowChat"));

        var result = await _handler.Handle(
            new AddContactCommand(ownerUserId, ownerUserId, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.BadRequest);
        result.Error.ErrorMessage.Should().Be("OwnerUserId and ContactUserId must be different.");
    }

    [Fact]
    public async Task Handle_WhenNamesAreMissing_UsesFriendlyUserIdAsDisplayName()
    {
        Contact? capturedContact = null;
        var ownerUserId = _fixture.Create<Guid>();
        var contactUserId = _fixture.Create<Guid>();

        _userProfileProjectionReadRepositoryMock
            .Setup(x => x.GetByUserProfileIdAsync(contactUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfileProjection(
                contactUserId,
                "fallback.user",
                "fallback@example.com",
                null,
                null,
                null,
                true,
                null));

        _contactWriteRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
            .Callback<Contact, CancellationToken>((contact, _) => capturedContact = contact)
            .ReturnsAsync((Contact contact, CancellationToken _) => contact);

        var result = await _handler.Handle(
            new AddContactCommand(ownerUserId, contactUserId, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedContact.Should().NotBeNull();
        capturedContact!.DisplayName.Should().Be("fallback.user");
    }
}
