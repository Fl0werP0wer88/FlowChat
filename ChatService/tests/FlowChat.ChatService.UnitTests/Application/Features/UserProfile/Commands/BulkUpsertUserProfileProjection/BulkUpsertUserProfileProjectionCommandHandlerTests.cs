using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.ChatService.Application.Features.UserProfile.Commands.BulkUpsertUserProfileProjection;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FluentAssertions;
using Moq;

namespace FlowChat.ChatService.UnitTests;

public sealed class BulkUpsertUserProfileProjectionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCommandIsValid_UpsertsNormalizedProjections()
    {
        IReadOnlyCollection<UserProfileProjectionDto>? capturedItems = null;
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var executorMock = new Mock<IBulkUpsertExecutor<UserProfileProjectionDto>>();
        executorMock
            .Setup(x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<UserProfileProjectionDto>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<UserProfileProjectionDto>, CancellationToken>((items, _) => capturedItems = items)
            .ReturnsAsync(FlowChatResult<BulkUpsertCommandResult>.Success(BulkUpsertCommandResult.FromRequestedCount(2)));
        var handler = new BulkUpsertUserProfileProjectionCommandHandler(unitOfWorkMock.Object, executorMock.Object);

        var result = await handler.Handle(
            new BulkUpsertUserProfileProjectionCommand(
                [
                    new BulkUpsertUserProfileProjectionCommandItem(Guid.NewGuid(), " jdoe ", " John ", " https://avatar "),
                    new BulkUpsertUserProfileProjectionCommandItem(Guid.NewGuid(), "asmith", " ", null)
                ]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UpsertedCount.Should().Be(2);
        capturedItems.Should().NotBeNull();
        capturedItems!.First().FriendlyUserId.Should().Be("jdoe");
        capturedItems.First().DisplayName.Should().Be("John");
        capturedItems.First().AvatarUrl.Should().Be("https://avatar");
        capturedItems.Last().DisplayName.Should().BeNull();
    }

    [Fact]
    public async Task Validate_WhenDuplicateUserProfileIds_ReturnsValidationError()
    {
        var userProfileId = Guid.NewGuid();
        var validator = new BulkUpsertUserProfileProjectionCommandValidator();

        var result = await validator.ValidateAsync(
            new BulkUpsertUserProfileProjectionCommand(
                [
                    new BulkUpsertUserProfileProjectionCommandItem(userProfileId, "jdoe", null, null),
                    new BulkUpsertUserProfileProjectionCommandItem(userProfileId, "john", null, null)
                ]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == "Payload contains duplicate UserProfileId values.");
    }

    private static Mock<IUnitOfWork> CreateUnitOfWorkMock()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<BulkUpsertCommandResult>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<BulkUpsertCommandResult>>>, CancellationToken>(
                (operation, cancellationToken) => operation(cancellationToken));

        return unitOfWorkMock;
    }
}
