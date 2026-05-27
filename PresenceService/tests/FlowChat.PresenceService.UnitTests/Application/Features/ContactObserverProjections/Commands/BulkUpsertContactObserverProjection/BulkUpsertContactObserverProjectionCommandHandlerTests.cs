using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.BulkUpsertContactObserverProjection;
using FlowChat.Shared.Application;
using FluentAssertions;
using Moq;

namespace FlowChat.PresenceService.UnitTests;

public sealed class BulkUpsertContactObserverProjectionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCommandIsValid_UpsertsProjectionsWithSingleTimestamp()
    {
        IReadOnlyCollection<ContactObserverProjectionDto>? capturedItems = null;
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var executorMock = new Mock<IBulkUpsertExecutor<ContactObserverProjectionDto>>();
        executorMock
            .Setup(x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<ContactObserverProjectionDto>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<ContactObserverProjectionDto>, CancellationToken>((items, _) => capturedItems = items)
            .ReturnsAsync(FlowChatResult<BulkUpsertCommandResult>.Success(BulkUpsertCommandResult.FromRequestedCount(2)));
        var handler = new BulkUpsertContactObserverProjectionCommandHandler(unitOfWorkMock.Object, executorMock.Object);

        var result = await handler.Handle(
            new BulkUpsertContactObserverProjectionCommand(
                [
                    new BulkUpsertContactObserverProjectionCommandItem(Guid.NewGuid(), Guid.NewGuid()),
                    new BulkUpsertContactObserverProjectionCommandItem(Guid.NewGuid(), Guid.NewGuid())
                ]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedItems.Should().NotBeNull();
        var firstTimestamp = capturedItems!.First().CreatedAtUtc;
        capturedItems.Should().OnlyContain(x =>
            x.CreatedAtUtc == firstTimestamp &&
            x.LastModifiedAtUtc == firstTimestamp);
    }

    [Fact]
    public async Task Validate_WhenDuplicateCompositeKeys_ReturnsValidationError()
    {
        var observedUserId = Guid.NewGuid();
        var observerUserId = Guid.NewGuid();
        var validator = new BulkUpsertContactObserverProjectionCommandValidator();

        var result = await validator.ValidateAsync(
            new BulkUpsertContactObserverProjectionCommand(
                [
                    new BulkUpsertContactObserverProjectionCommandItem(observedUserId, observerUserId),
                    new BulkUpsertContactObserverProjectionCommandItem(observedUserId, observerUserId)
                ]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == "Payload contains duplicate contact observer projection keys.");
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
