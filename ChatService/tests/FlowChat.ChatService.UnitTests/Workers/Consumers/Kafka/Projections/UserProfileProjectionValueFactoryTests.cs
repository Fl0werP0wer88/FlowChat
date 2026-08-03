using AutoMapper;
using FlowChat.ChatService.Consumers.Kafka;
using FlowChat.ChatService.Consumers.Kafka.Projections;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowChat.ChatService.UnitTests.Workers.Consumers.Kafka.Projections;

public sealed class UserProfileProjectionValueFactoryTests
{
    private readonly UserProfileProjectionValueFactory _factory;

    public UserProfileProjectionValueFactoryTests()
    {
        var mapperConfiguration = new MapperConfiguration(
            configuration => configuration.AddProfile<UserProfileProjectionRequestProfile>(),
            NullLoggerFactory.Instance);
        _factory = new UserProfileProjectionValueFactory(mapperConfiguration.CreateMapper());
    }

    [Fact]
    public void MapValue_WhenEventIsUpsert_MapsReadModelFields()
    {
        var userProfileId = Guid.NewGuid();

        var value = _factory.MapValue(CreateProjectionEvent(
            OperationType.Updated,
            userProfileId,
            new UserProfileReadModel
            {
                UserProfileId = userProfileId,
                FriendlyUserId = "jdoe",
                FirstName = "John",
                LastName = "Doe",
                AvatarUrl = "https://avatar",
                MainEmail = new FlowChat.Core.Messaging.UserProfileService.Events.UserProfileEmail
                {
                    Address = "jdoe@example.com",
                    IsConfirmed = true,
                    IsVisible = false
                }
            }));

        value.UserProfileId.Should().Be(userProfileId);
        value.FriendlyUserId.Should().Be("jdoe");
        value.FirstName.Should().Be("John");
        value.LastName.Should().Be("Doe");
        value.AvatarUrl.Should().Be("https://avatar");
        value.Email.Should().Be("jdoe@example.com");
        value.Source.Should().Be("user-profile-projection");
    }

    [Fact]
    public void MapValue_WhenMainEmailIsNull_MapsEmailAsNull()
    {
        var userProfileId = Guid.NewGuid();

        var value = _factory.MapValue(CreateProjectionEvent(
            OperationType.Updated,
            userProfileId,
            new UserProfileReadModel
            {
                UserProfileId = userProfileId,
                FriendlyUserId = "jdoe"
            }));

        value.Email.Should().BeNull();
    }

    [Fact]
    public void MapValue_WhenEventIsDelete_MapsFullReadModelPayload()
    {
        var userProfileId = Guid.NewGuid();

        var value = _factory.MapValue(CreateProjectionEvent(
            OperationType.Deleted,
            userProfileId,
            new UserProfileReadModel
            {
                UserProfileId = userProfileId,
                FriendlyUserId = "deleted-user",
                FirstName = "Deleted",
                LastName = "Profile"
            }));

        value.UserProfileId.Should().Be(userProfileId);
        value.FriendlyUserId.Should().Be("deleted-user");
        value.FirstName.Should().Be("Deleted");
        value.LastName.Should().Be("Profile");
        value.Source.Should().Be("user-profile-projection");
    }

    [Fact]
    public void MapValue_WhenUpsertHasInvalidFriendlyUserId_ThrowsNonTransientException()
    {
        var userProfileId = Guid.NewGuid();

        var act = () => _factory.MapValue(CreateProjectionEvent(
            OperationType.Updated,
            userProfileId,
            new UserProfileReadModel
            {
                UserProfileId = userProfileId,
                FriendlyUserId = "   "
            }));

        act.Should().Throw<NonTransientException>();
    }

    private static ProjectionIntegrationEvent<UserProfileReadModel> CreateProjectionEvent(
        OperationType operation,
        Guid sourceAggregateId,
        UserProfileReadModel value) =>
        new()
        {
            SourceAggregateId = sourceAggregateId,
            SourceAggregateCreatedAtUtc = DateTimeOffset.UtcNow,
            SourceAggregateModifiedAtUtc = DateTimeOffset.UtcNow,
            Operation = operation,
            SourceAggregateVersion = 1,
            Value = value
        };
}
