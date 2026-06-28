using AutoMapper;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.SocialGraphService.Consumers.Kafka;
using FlowChat.SocialGraphService.Consumers.Kafka.Projections;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowChat.SocialGraphService.UnitTests.Workers.Consumers.Kafka.Projections;

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
                Organization = "Acme",
                MainEmail = new UserProfileEmail { Address = "john@example.com", IsConfirmed = true, IsVisible = true },
                MainPhone = new UserProfilePhone { Number = "+48123123123", IsConfirmed = false, IsVisible = false },
                AvatarUrl = "https://avatar",
                Bio = "Bio",
                IsActive = true
            },
            sourceVersion: 5));

        value.UserProfileId.Should().Be(userProfileId);
        value.FriendlyUserId.Should().Be("jdoe");
        value.FirstName.Should().Be("John");
        value.LastName.Should().Be("Doe");
        value.Organization.Should().Be("Acme");
        value.MainEmail.Should().NotBeNull();
        value.MainEmail!.Address.Should().Be("john@example.com");
        value.MainEmail.IsConfirmed.Should().BeTrue();
        value.MainPhone.Should().NotBeNull();
        value.MainPhone!.Number.Should().Be("+48123123123");
        value.AvatarUrl.Should().Be("https://avatar");
        value.Bio.Should().Be("Bio");
        value.SourceVersion.Should().Be(5);
        value.Source.Should().Be("user-profile-projection");
    }

    [Fact]
    public void MapValue_WhenEventIsDelete_UsesSourceAggregateIdAndEmptyFriendlyUserId()
    {
        var userProfileId = Guid.NewGuid();

        var value = _factory.MapValue(CreateProjectionEvent(
            OperationType.Deleted,
            userProfileId,
            new UserProfileReadModel
            {
                UserProfileId = Guid.Empty,
                FriendlyUserId = string.Empty
            },
            sourceVersion: 2));

        value.UserProfileId.Should().Be(userProfileId);
        value.FriendlyUserId.Should().BeEmpty();
        value.IsActive.Should().BeFalse();
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
            },
            sourceVersion: 1));

        act.Should().Throw<NonTransientException>();
    }

    [Fact]
    public void GetDeduplicationKey_ReturnsUserProfileId()
    {
        var userProfileId = Guid.NewGuid();
        var value = _factory.MapValue(CreateProjectionEvent(
            OperationType.Updated,
            userProfileId,
            new UserProfileReadModel
            {
                UserProfileId = userProfileId,
                FriendlyUserId = "jdoe"
            },
            sourceVersion: 1));

        _factory.GetDeduplicationKey(value).Should().Be(userProfileId);
    }

    private static ProjectionIntegrationEvent<UserProfileReadModel> CreateProjectionEvent(
        OperationType operation,
        Guid sourceAggregateId,
        UserProfileReadModel value,
        int sourceVersion) =>
        new()
        {
            SourceAggregateId = sourceAggregateId,
            SourceAggregateCreatedAtUtc = DateTimeOffset.UtcNow,
            SourceAggregateModifiedAtUtc = DateTimeOffset.UtcNow,
            Operation = operation,
            SourceAggregateVersion = sourceVersion,
            Value = value
        };
}
