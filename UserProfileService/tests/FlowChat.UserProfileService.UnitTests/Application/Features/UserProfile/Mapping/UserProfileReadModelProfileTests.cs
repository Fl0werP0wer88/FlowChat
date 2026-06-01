using AutoMapper;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Features.UserProfile.Mapping;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class UserProfileReadModelProfileTests
{
    private readonly IMapper _mapper;

    public UserProfileReadModelProfileTests()
    {
        _mapper = new MapperConfiguration(
                configuration => configuration.AddProfile<UserProfileReadModelProfile>(),
                NullLoggerFactory.Instance)
            .CreateMapper();
    }

    [Fact]
    public void Map_UserProfileWithMainContacts_ReturnsUserProfileReadModel()
    {
        var userProfileId = Id<UserProfile>.New();
        var lastSeenAtUtc = new DateTimeOffset(2026, 5, 12, 8, 30, 0, TimeSpan.Zero);
        var userProfile = UserProfile.Create(
            userProfileId,
            "jdoe",
            EmailAddress.Create("john@example.com"),
            PhoneNumber.Create("+48123123123"),
            avatarUrl: "https://cdn.example/avatar.png",
            bio: "about me",
            isActive: true,
            lastSeenAtUtc: UtcDateTimeOffset.Create(lastSeenAtUtc),
            firstName: "John",
            lastName: "Doe",
            organization: "FlowChat");

        var readModel = _mapper.Map<UserProfileReadModel>(userProfile);

        readModel.UserProfileId.Should().Be(userProfileId.Value);
        readModel.FriendlyUserId.Should().Be("jdoe");
        readModel.FirstName.Should().Be("John");
        readModel.LastName.Should().Be("Doe");
        readModel.Organization.Should().Be("FlowChat");
        readModel.MainEmail.Should().NotBeNull();
        readModel.MainEmail!.Address.Should().Be("john@example.com");
        readModel.MainEmail.IsConfirmed.Should().BeFalse();
        readModel.MainEmail.IsVisible.Should().BeTrue();
        readModel.MainPhone.Should().NotBeNull();
        readModel.MainPhone!.Number.Should().Be("+48123123123");
        readModel.MainPhone.IsConfirmed.Should().BeFalse();
        readModel.MainPhone.IsVisible.Should().BeTrue();
        readModel.AvatarUrl.Should().Be("https://cdn.example/avatar.png");
        readModel.Bio.Should().Be("about me");
        readModel.IsActive.Should().BeTrue();
        readModel.LastSeenAtUtc.Should().Be(lastSeenAtUtc);
    }

    [Fact]
    public void Map_UserProfileWithoutOptionalFields_ReturnsNullOptionalProjectionValues()
    {
        var userProfile = UserProfile.Create(
            Id<UserProfile>.New(),
            "jdoe",
            EmailAddress.Create("john@example.com"));

        var readModel = _mapper.Map<UserProfileReadModel>(userProfile);

        readModel.FirstName.Should().BeNull();
        readModel.LastName.Should().BeNull();
        readModel.Organization.Should().BeNull();
        readModel.MainEmail.Should().NotBeNull();
        readModel.MainPhone.Should().BeNull();
        readModel.AvatarUrl.Should().BeNull();
        readModel.Bio.Should().BeNull();
        readModel.LastSeenAtUtc.Should().BeNull();
    }
}
