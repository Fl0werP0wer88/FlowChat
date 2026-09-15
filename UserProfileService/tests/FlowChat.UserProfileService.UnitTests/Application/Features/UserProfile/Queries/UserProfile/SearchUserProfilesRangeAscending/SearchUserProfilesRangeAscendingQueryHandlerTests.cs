using AutoFixture;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.SearchUserProfilesRangeAscending;
using FluentAssertions;
using Moq;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class SearchUserProfilesRangeAscendingQueryHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IUserProfileReadRepository> _repositoryMock = new();

    [Fact]
    public async Task Handle_WhenMoreRowsThanLimitExist_ReturnsPageAndNormalizedNextCursor()
    {
        IReadOnlyList<UserProfileSearchResultDto> rows =
        [
            CreateProfile("alice"),
            CreateProfile("bob"),
            CreateProfile("charlie")
        ];
        _repositoryMock
            .Setup(repository => repository.SearchRangeAscendingAsync(
                "Jane",
                null,
                "Flow",
                "alice",
                3,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);
        var handler = new SearchUserProfilesRangeAscendingQueryHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new SearchUserProfilesRangeAscendingQuery(" Jane ", "   ", " Flow ", " ALICE ", 2),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Select(item => item.FriendlyUserId).Should().Equal("alice", "bob");
        result.Value.HasMore.Should().BeTrue();
        result.Value.NextCursor.Should().Be("bob");
    }

    [Fact]
    public async Task Handle_WhenFinalPageIsReturned_DoesNotReturnNextCursor()
    {
        IReadOnlyList<UserProfileSearchResultDto> rows = [CreateProfile("alice")];
        _repositoryMock
            .Setup(repository => repository.SearchRangeAscendingAsync(
                "Jane",
                null,
                null,
                null,
                21,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);
        var handler = new SearchUserProfilesRangeAscendingQueryHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new SearchUserProfilesRangeAscendingQuery("Jane", null, null, null, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEquivalentTo(rows);
        result.Value.HasMore.Should().BeFalse();
        result.Value.NextCursor.Should().BeNull();
    }

    private UserProfileSearchResultDto CreateProfile(string friendlyUserId) =>
        new(_fixture.Create<Guid>(), friendlyUserId, "Jane", "Doe", "FlowChat", null);
}
