using AutoFixture;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Application.Features.UserProfile.Queries.SearchUserProfileProjections;
using FluentAssertions;
using Moq;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class SearchUserProfileProjectionsQueryHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IUserProfileProjectionReadRepository> _userProfileProjectionReadRepositoryMock = new();
    private readonly SearchUserProfileProjectionsQueryHandler _handler;

    public SearchUserProfileProjectionsQueryHandlerTests()
    {
        _handler = new SearchUserProfileProjectionsQueryHandler(_userProfileProjectionReadRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenMatchingProjectionsExist_ReturnsSuccessWithTrimmedSearchArguments()
    {
        IReadOnlyList<UserProfileProjectionDto> expectedProjections =
        [
            new UserProfileProjectionDto
            {
                UserProfileId = _fixture.Create<Guid>(),
                FriendlyUserId = "jdoe",
                FirstName = "Jane",
                LastName = "Doe",
                Organization = "FlowChat",
                MainEmail = new UserProfileProjectionEmailDto
                {
                    Address = "jane@example.com",
                    IsConfirmed = true,
                    IsVisible = true
                },
                MainPhone = new UserProfileProjectionPhoneDto
                {
                    Number = "+48123123123",
                    IsConfirmed = true,
                    IsVisible = true
                },
                IsActive = true,
                LastSeenAtUtc = _fixture.Create<DateTimeOffset>()
            }
        ];

        _userProfileProjectionReadRepositoryMock
            .Setup(x => x.SearchAsync("Jane", null, "Flow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProjections);

        var result = await _handler.Handle(
            new SearchUserProfileProjectionsQuery(" Jane ", "   ", " Flow "),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedProjections);
        _userProfileProjectionReadRepositoryMock.Verify(
            x => x.SearchAsync("Jane", null, "Flow", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

