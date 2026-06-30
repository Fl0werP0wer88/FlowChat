using AutoFixture;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.GetUserProfiles;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;
using FluentAssertions;
using Moq;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class GetUserProfilesQueryHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IUserProfileReadRepository> _userProfileReadRepositoryMock = new();
    private readonly GetUserProfilesQueryHandler _handler;

    public GetUserProfilesQueryHandlerTests()
    {
        _handler = new GetUserProfilesQueryHandler(_userProfileReadRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenProfilesExist_ReturnsSuccessWithProfiles()
    {
        var firstUserId = _fixture.Create<Guid>();
        var secondUserId = _fixture.Create<Guid>();
        IReadOnlyList<UserProfileDto> expectedUserProfiles =
        [
            new UserProfileDto(firstUserId, "jdoe", null, null, null, null, null, true, null, [], []),
            new UserProfileDto(secondUserId, "asmith", null, null, null, null, null, true, null, [], [])
        ];

        _userProfileReadRepositoryMock
            .Setup(x => x.GetByIdsAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { firstUserId, secondUserId })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUserProfiles);

        var result = await _handler.Handle(
            new GetUserProfilesQuery([firstUserId, secondUserId]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedUserProfiles);
        _userProfileReadRepositoryMock.Verify(
            x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserIdsContainDuplicatesAndEmptyValues_NormalizesArguments()
    {
        var firstUserId = _fixture.Create<Guid>();
        var secondUserId = _fixture.Create<Guid>();
        IReadOnlyList<Guid>? capturedUserIds = null;

        _userProfileReadRepositoryMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<Guid>, CancellationToken>((ids, _) => capturedUserIds = ids.ToList())
            .ReturnsAsync([]);

        var result = await _handler.Handle(
            new GetUserProfilesQuery([firstUserId, Guid.Empty, secondUserId, firstUserId]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedUserIds.Should().Equal(firstUserId, secondUserId);
    }
}
