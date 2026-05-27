using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.BulkUpsertUserProfileProjection;
using FluentAssertions;
using Moq;

namespace FlowChat.SocialGraphService.UnitTests;

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
            .ReturnsAsync(FlowChatResult<BulkUpsertCommandResult>.Success(BulkUpsertCommandResult.FromRequestedCount(1)));
        var handler = new BulkUpsertUserProfileProjectionCommandHandler(unitOfWorkMock.Object, executorMock.Object);

        var result = await handler.Handle(
            new BulkUpsertUserProfileProjectionCommand(
                [
                    new BulkUpsertUserProfileProjectionCommandItem(
                        Guid.NewGuid(),
                        " jdoe ",
                        " John ",
                        " Doe ",
                        " FlowChat ",
                        " john@example.com ",
                        true,
                        true,
                        " +48123123123 ",
                        false,
                        true,
                        " https://avatar ",
                        " hello ",
                        true,
                        DateTimeOffset.UtcNow)
                ]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedItems.Should().NotBeNull();
        var projection = capturedItems!.Single();
        projection.FriendlyUserId.Should().Be("jdoe");
        projection.FirstName.Should().Be("John");
        projection.LastName.Should().Be("Doe");
        projection.Organization.Should().Be("FlowChat");
        projection.MainEmail!.Address.Should().Be("john@example.com");
        projection.MainPhone!.Number.Should().Be("+48123123123");
        projection.AvatarUrl.Should().Be("https://avatar");
        projection.Bio.Should().Be("hello");
    }

    [Fact]
    public async Task Validate_WhenDuplicateUserProfileIds_ReturnsValidationError()
    {
        var userProfileId = Guid.NewGuid();
        var validator = new BulkUpsertUserProfileProjectionCommandValidator();

        var result = await validator.ValidateAsync(
            new BulkUpsertUserProfileProjectionCommand(
                [
                    CreateItem(userProfileId),
                    CreateItem(userProfileId)
                ]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == "Payload contains duplicate UserProfileId values.");
    }

    private static BulkUpsertUserProfileProjectionCommandItem CreateItem(Guid userProfileId) =>
        new(userProfileId, "jdoe", null, null, null, null, null, null, null, null, null, null, null, true, null);

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
