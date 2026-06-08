using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class BulkUpsertOrDeleteUserProfileProjectionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCommandIsValid_CallsBulkRepository()
    {
        IReadOnlyCollection<UserProfileProjectionCommandItem>? capturedItems = null;
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var repositoryMock = new Mock<IUserProfileProjectionBulkRepository>();
        repositoryMock
            .Setup(x => x.BulkUpsertOrSoftDeleteAsync(It.IsAny<IReadOnlyCollection<UserProfileProjectionCommandItem>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<UserProfileProjectionCommandItem>, CancellationToken>((items, _) => capturedItems = items)
            .Returns(Task.CompletedTask);
        var handler = new BulkUpsertOrDeleteUserProfileProjectionCommandHandler(unitOfWorkMock.Object, repositoryMock.Object);
        var userProfileId = Guid.NewGuid();

        var result = await handler.Handle(
            new BulkUpsertOrDeleteUserProfileProjectionCommand(
                [
                    CreateUpsertItem(userProfileId, 1, friendlyUserId: "jdoe")
                ]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedItems.Should().NotBeNull();
        capturedItems!.Should().ContainSingle().Which.EntityId.Should().Be(Id<UserProfileProjectionDto>.FromGuid(userProfileId));
    }

    [Fact]
    public async Task Validate_WhenDuplicateUserProfileIds_ReturnsValidationError()
    {
        var userProfileId = Guid.NewGuid();
        var validator = new BulkUpsertOrDeleteUserProfileProjectionCommandValidator();

        var result = await validator.ValidateAsync(
            new BulkUpsertOrDeleteUserProfileProjectionCommand(
                [
                    CreateUpsertItem(userProfileId, 1),
                    CreateUpsertItem(userProfileId, 2)
                ]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == "Payload contains duplicate UserProfileId values.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Validate_WhenSourceVersionIsInvalid_ReturnsValidationError(int sourceVersion)
    {
        var validator = new BulkUpsertOrDeleteUserProfileProjectionCommandValidator();

        var result = await validator.ValidateAsync(
            new BulkUpsertOrDeleteUserProfileProjectionCommand(
                [
                    CreateUpsertItem(Guid.NewGuid(), sourceVersion)
                ]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == "Item does not contain a valid SourceVersion.");
    }

    [Fact]
    public async Task Validate_WhenUpsertFriendlyUserIdIsInvalid_ReturnsValidationError()
    {
        var validator = new BulkUpsertOrDeleteUserProfileProjectionCommandValidator();

        var result = await validator.ValidateAsync(
            new BulkUpsertOrDeleteUserProfileProjectionCommand(
                [
                    CreateUpsertItem(Guid.NewGuid(), 1, friendlyUserId: " ")
                ]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == "Upsert item does not contain a valid FriendlyUserId.");
    }

    [Fact]
    public async Task Validate_WhenUpsertSourceIsInvalid_ReturnsValidationError()
    {
        var validator = new BulkUpsertOrDeleteUserProfileProjectionCommandValidator();

        var result = await validator.ValidateAsync(
            new BulkUpsertOrDeleteUserProfileProjectionCommand(
                [
                    CreateUpsertItem(Guid.NewGuid(), 1, source: " ")
                ]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == "Upsert item does not contain a valid Source.");
    }

    private static UserProfileProjectionCommandItem CreateUpsertItem(
        Guid userProfileId,
        int sourceVersion,
        string friendlyUserId = "jdoe",
        string source = "user-profile-projection") =>
        new(
            Id<UserProfileProjectionDto>.FromGuid(userProfileId),
            new UserProfileProjectionDto
            {
                UserProfileId = userProfileId,
                FriendlyUserId = friendlyUserId,
                IsActive = true,
                SourceVersion = sourceVersion,
                Source = source
            },
            sourceVersion,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null);

    private static Mock<IUnitOfWork> CreateUnitOfWorkMock()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>(
                (operation, cancellationToken) => operation(cancellationToken));

        return unitOfWorkMock;
    }
}
