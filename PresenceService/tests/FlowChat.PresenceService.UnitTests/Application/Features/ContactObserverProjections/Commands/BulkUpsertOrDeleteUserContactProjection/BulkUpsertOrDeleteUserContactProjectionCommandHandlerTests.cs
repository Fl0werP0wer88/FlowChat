using FlowChat.Core.Messaging;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.BulkUpsertOrDeleteUserContactProjection;
using FlowChat.Shared.Application;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.PresenceService.UnitTests.Application.Features.ContactObserverProjections.Commands.BulkUpsertOrDeleteUserContactProjection;

public sealed class BulkUpsertOrDeleteUserContactProjectionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCommandIsValid_CallsBulkRepository()
    {
        IReadOnlyCollection<UserContactProjectionCommandItem>? capturedItems = null;
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var repositoryMock = new Mock<IContactObserverProjectionBulkRepository>();
        repositoryMock
            .Setup(x => x.BulkUpsertOrSoftDeleteAsync(
                It.IsAny<IReadOnlyCollection<UserContactProjectionCommandItem>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<UserContactProjectionCommandItem>, CancellationToken>((items, _) => capturedItems = items)
            .Returns(Task.CompletedTask);
        var handler = new BulkUpsertOrDeleteUserContactProjectionCommandHandler(unitOfWorkMock.Object, repositoryMock.Object);

        var result = await handler.Handle(
            new BulkUpsertOrDeleteUserContactProjectionCommand(
                [
                    CreateUpsertItem(Guid.NewGuid(), Guid.NewGuid(), 1),
                    CreateDeleteItem(Guid.NewGuid(), Guid.NewGuid(), 2)
                ]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedItems.Should().NotBeNull();
        capturedItems.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WhenBatchIsEmpty_ReturnsSuccessWithoutCallingRepository()
    {
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var repositoryMock = new Mock<IContactObserverProjectionBulkRepository>();
        var handler = new BulkUpsertOrDeleteUserContactProjectionCommandHandler(unitOfWorkMock.Object, repositoryMock.Object);

        var result = await handler.Handle(
            new BulkUpsertOrDeleteUserContactProjectionCommand([]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repositoryMock.Verify(
            x => x.BulkUpsertOrSoftDeleteAsync(
                It.IsAny<IReadOnlyCollection<UserContactProjectionCommandItem>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Validate_WhenDuplicateCompositeKeys_ReturnsValidationError()
    {
        var observedUserId = Guid.NewGuid();
        var observerUserId = Guid.NewGuid();
        var validator = new BulkUpsertOrDeleteUserContactProjectionCommandValidator();

        var result = await validator.ValidateAsync(
            new BulkUpsertOrDeleteUserContactProjectionCommand(
                [
                    CreateUpsertItem(observedUserId, observerUserId, 1),
                    CreateDeleteItem(observedUserId, observerUserId, 2)
                ]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == "Payload contains duplicate contact observer projection keys.");
    }

    [Fact]
    public async Task Validate_WhenSourceVersionIsInvalid_ReturnsValidationError()
    {
        var validator = new BulkUpsertOrDeleteUserContactProjectionCommandValidator();

        var result = await validator.ValidateAsync(
            new BulkUpsertOrDeleteUserContactProjectionCommand(
                [CreateUpsertItem(Guid.NewGuid(), Guid.NewGuid(), 0)]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == "Item does not contain a valid SourceVersion.");
    }

    [Fact]
    public async Task Validate_WhenUpsertSourceIsEmpty_ReturnsValidationError()
    {
        var validator = new BulkUpsertOrDeleteUserContactProjectionCommandValidator();

        var result = await validator.ValidateAsync(
            new BulkUpsertOrDeleteUserContactProjectionCommand(
                [
                    new UserContactProjectionCommandItem(
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        new ContactObserverProjectionDto { Source = " " },
                        OperationType.Updated,
                        1,
                        DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow,
                        null)
                ]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == "Upsert item does not contain a valid Source.");
    }

    private static UserContactProjectionCommandItem CreateUpsertItem(
        Guid observedUserId,
        Guid observerUserId,
        int sourceVersion,
        string source = "consumer") =>
        new(
            observedUserId,
            observerUserId,
            new ContactObserverProjectionDto
            {
                ObservedUserId = observedUserId,
                ObserverUserId = observerUserId,
                SourceVersion = sourceVersion,
                Source = source
            },
            OperationType.Updated,
            sourceVersion,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null);

    private static UserContactProjectionCommandItem CreateDeleteItem(
        Guid observedUserId,
        Guid observerUserId,
        int sourceVersion) =>
        new(
            observedUserId,
            observerUserId,
            null,
            OperationType.Deleted,
            sourceVersion,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

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
