using FlowChat.Application.Abstractions;
using FlowChat.Messaging.Contracts.UserProfileService.Events;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.UserProfiles;
using FlowChat.SocialGraphService.Worker.Kafka;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class UserProfileSubscriberTests
{
    [Fact]
    public async Task HandleAsync_WhenProfileCreatedEventArrives_CreatesReadModel()
    {
        var repository = new FakeUserProfileReadModelRepository(upsertResult: true);
        var unitOfWork = new FakeUnitOfWork();
        var subscriber = new UserProfileSubscriber(
            repository,
            unitOfWork,
            NullLogger<UserProfileSubscriber>.Instance);

        var userProfileId = Guid.NewGuid();
        await subscriber.HandleAsync(
            new UserProfileCreatedIntegrationEvent
            {
                UserProfileId = userProfileId,
                UserName = " john.doe ",
                DisplayName = " John Doe ",
                MainEmail = " john@flowchat.local ",
                MainPhone = " +48123123123 ",
                AvatarUrl = " https://cdn.example/avatar.png ",
                Bio = " hello ",
                IsActive = true,
                LastSeenAtUtc = new DateTime(2026, 3, 11, 10, 0, 0, DateTimeKind.Utc),
                IsEmailVisible = true,
                IsPhoneVisible = false
            },
            CancellationToken.None);

        Assert.NotNull(repository.LastUpsertedReadModel);
        Assert.Equal(userProfileId, repository.LastUpsertedReadModel.UserProfileId);
        Assert.Equal("john.doe", repository.LastUpsertedReadModel.UserName);
        Assert.Equal("John Doe", repository.LastUpsertedReadModel.DisplayName);
        Assert.Equal("john@flowchat.local", repository.LastUpsertedReadModel.MainEmail);
        Assert.Equal("+48123123123", repository.LastUpsertedReadModel.MainPhone);
        Assert.Equal("https://cdn.example/avatar.png", repository.LastUpsertedReadModel.AvatarUrl);
        Assert.Equal("hello", repository.LastUpsertedReadModel.Bio);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenStateChangedEventArrives_UpdatesReadModel()
    {
        var repository = new FakeUserProfileReadModelRepository(upsertResult: false);
        var unitOfWork = new FakeUnitOfWork();
        var subscriber = new UserProfileSubscriber(
            repository,
            unitOfWork,
            NullLogger<UserProfileSubscriber>.Instance);

        var userProfileId = Guid.NewGuid();
        await subscriber.HandleAsync(
            new UserProfileStateChangedIntegrationEvent
            {
                UserProfileId = userProfileId,
                UserName = "jane.doe",
                DisplayName = "Jane Doe",
                MainEmail = null,
                MainPhone = "123456",
                AvatarUrl = null,
                Bio = "updated",
                IsActive = false,
                LastSeenAtUtc = null,
                IsEmailVisible = false,
                IsPhoneVisible = true
            },
            CancellationToken.None);

        Assert.NotNull(repository.LastUpsertedReadModel);
        Assert.Equal("jane.doe", repository.LastUpsertedReadModel.UserName);
        Assert.Equal("Jane Doe", repository.LastUpsertedReadModel.DisplayName);
        Assert.False(repository.LastUpsertedReadModel.IsActive);
        Assert.True(repository.LastUpsertedReadModel.IsPhoneVisible);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenUserProfileIdIsMissing_ThrowsInvalidOperationException()
    {
        var repository = new FakeUserProfileReadModelRepository(upsertResult: true);
        var unitOfWork = new FakeUnitOfWork();
        var subscriber = new UserProfileSubscriber(
            repository,
            unitOfWork,
            NullLogger<UserProfileSubscriber>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => subscriber.HandleAsync(
            new UserProfileCreatedIntegrationEvent
            {
                UserProfileId = Guid.Empty,
                UserName = "john.doe",
                DisplayName = "John Doe"
            },
            CancellationToken.None));

        Assert.Null(repository.LastUpsertedReadModel);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private sealed class FakeUserProfileReadModelRepository(bool upsertResult) : IUserProfileReadModelRepository
    {
        public UserProfileReadModel? LastUpsertedReadModel { get; private set; }

        public Task<bool> UpsertAsync(UserProfileReadModel readModel, CancellationToken cancellationToken = default)
        {
            LastUpsertedReadModel = readModel;
            return Task.FromResult(upsertResult);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public void Dispose()
        {
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken)
        {
            var result = await operation(cancellationToken);
            SaveChangesCallCount++;
            return result;
        }
    }
}
