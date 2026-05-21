using AutoFixture;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.SearchUserProfiles;
using FluentAssertions;
using Moq;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class SearchUserProfilesQueryHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IUserProfileReadRepository> _userProfileReadRepositoryMock = new();
    private readonly SearchUserProfilesQueryHandler _handler;

    public SearchUserProfilesQueryHandlerTests()
    {
        _handler = new SearchUserProfilesQueryHandler(_userProfileReadRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenMatchingProfilesExist_ReturnsSuccessWithTrimmedSearchArguments()
    {
        IReadOnlyList<UserProfileDto> expectedUserProfiles =
        [
            new UserProfileDto
            {
                Id = _fixture.Create<Guid>(),
                FriendlyUserId = "jdoe",
                FirstName = "Jane",
                LastName = "Doe",
                Organization = "FlowChat",
                MainEmail = new EmailDto
                {
                    Address = "jane@example.com",
                    IsConfirmed = true,
                    IsVisible = true
                },
                MainPhone = new PhoneDto
                {
                    Number = "+48123123123",
                    IsConfirmed = true,
                    IsVisible = true
                },
                IsActive = true,
                LastSeenAtUtc = _fixture.Create<DateTimeOffset>()
            }
        ];

        _userProfileReadRepositoryMock
            .Setup(x => x.SearchAsync("Jane", null, "Flow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUserProfiles);

        var result = await _handler.Handle(
            new SearchUserProfilesQuery(" Jane ", "   ", " Flow "),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedUserProfiles);
        _userProfileReadRepositoryMock.Verify(
            x => x.SearchAsync("Jane", null, "Flow", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
