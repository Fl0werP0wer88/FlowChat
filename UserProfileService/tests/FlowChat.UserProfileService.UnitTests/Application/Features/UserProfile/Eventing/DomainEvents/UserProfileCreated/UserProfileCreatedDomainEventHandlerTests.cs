using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification.Interfaces;
using FlowChat.UserProfileService.Application.Features.UserProfile.Eventing.DomainEvents.UserProfileCreated;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationProcess;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;
using DomainEmail = FlowChat.UserProfileService.Domain.Entities.UserProfile.Email;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class UserProfileCreatedDomainEventHandlerTests
{
    private readonly Mock<IEmailVerificationProcessWriteRepository> _repositoryMock = new();
    private readonly Mock<IEmailVerificationRequestIssuer> _issuerMock = new();

    public UserProfileCreatedDomainEventHandlerTests()
    {
        _repositoryMock
            .Setup(x => x.GetByEmailIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailVerificationProcess?)null);

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<EmailVerificationProcess>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailVerificationProcess entity, CancellationToken _) => entity);

        _issuerMock
            .Setup(x => x.IssueAsync(
                It.IsAny<EmailVerificationProcess>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailVerificationProcess _, Guid userProfileId, Guid emailId, string _, CancellationToken _) =>
                EmailVerificationProcess
                    .Create(Id<UserProfile>.FromGuid(userProfileId), Id<DomainEmail>.FromGuid(emailId))
                    .IssueRequest(
                        Id<EmailVerificationRequest>.New(),
                        Guid.NewGuid().ToString("N"),
                        DateTimeOffset.UtcNow.AddHours(24),
                        DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task Handle_WithNewProcess_CreatesProcessAndIssuesVerificationRequest()
    {
        var handler = new UserProfileCreatedDomainEventHandler(_repositoryMock.Object, _issuerMock.Object);
        var userProfileId = Id<UserProfile>.New();
        var mainEmailId = Id<DomainEmail>.New();
        var domainEvent = new UserProfileCreatedDomainEvent(
            userProfileId,
            mainEmailId,
            Id<Phone>.New(),
            "jdoe",
            EmailAddress.Create("john@example.com"),
            PhoneNumber.Create("+48123123123"),
            "https://cdn.example/avatar.png",
            "about me",
            true,
            new DateTimeOffset(2026, 3, 10, 9, 0, 0, TimeSpan.Zero),
            "John",
            "Doe",
            "FlowChat");

        await handler.Handle(domainEvent, CancellationToken.None);

        _repositoryMock.Verify(x => x.GetByEmailIdAsync(mainEmailId.Value, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(x => x.AddAsync(It.Is<EmailVerificationProcess>(process => process.Id.Value == mainEmailId.Value), It.IsAny<CancellationToken>()), Times.Once);
        _issuerMock.Verify(x => x.IssueAsync(
            It.Is<EmailVerificationProcess>(process => process.Id.Value == mainEmailId.Value),
            userProfileId.Value,
            mainEmailId.Value,
            "john@example.com",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingProcess_PassesExistingProcessToIssuer()
    {
        var userProfileId = Id<UserProfile>.New();
        var mainEmailId = Id<DomainEmail>.New();
        var process = EmailVerificationProcess.Create(userProfileId, mainEmailId);
        var handler = new UserProfileCreatedDomainEventHandler(_repositoryMock.Object, _issuerMock.Object);
        var domainEvent = new UserProfileCreatedDomainEvent(
            userProfileId,
            mainEmailId,
            null,
            "jdoe",
            EmailAddress.Create("john@example.com"),
            null,
            null,
            null,
            true,
            null);

        _repositoryMock
            .Setup(x => x.GetByEmailIdAsync(mainEmailId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);

        await handler.Handle(domainEvent, CancellationToken.None);

        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<EmailVerificationProcess>(), It.IsAny<CancellationToken>()), Times.Never);
        _issuerMock.Verify(x => x.IssueAsync(
            process,
            userProfileId.Value,
            mainEmailId.Value,
            "john@example.com",
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
