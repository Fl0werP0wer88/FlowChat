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
            new UserProfileDto(
                _fixture.Create<Guid>(),
                "jdoe",
                "Jane",
                "Doe",
                "FlowChat",
                null,
                null,
                true,
                _fixture.Create<DateTimeOffset>(),
                [new EmailDto(Guid.NewGuid(), "jane@example.com", true, false, true, true)],
                [new PhoneDto(Guid.NewGuid(), "+48123123123", true, true, true)])
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
