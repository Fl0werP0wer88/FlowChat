using AutoFixture;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.GetUserProfile;
using Moq;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class GetUserProfileQueryHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IUserProfileReadRepository> _readRepositoryMock = new();
    private readonly GetUserProfileQueryHandler _handler;

    public GetUserProfileQueryHandlerTests()
    {
        _handler = new GetUserProfileQueryHandler(_readRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenProfileExists_ReturnsSuccessWithProfile()
    {
        var userId = _fixture.Create<Guid>();
        var expectedProfile = new UserProfileDto(
            userId,
            "jdoe",
            "https://cdn.example/avatar.png",
            "about me",
            true,
            new DateTimeOffset(2026, 4, 1, 8, 0, 0, TimeSpan.Zero),
            [new EmailDto(Guid.NewGuid(), "john@example.com", true, true, false)],
            [new PhoneDto(Guid.NewGuid(), "+48123123123", true)]);

        _readRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProfile);

        var result = await _handler.Handle(new GetUserProfileQuery(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedProfile);
    }

    [Fact]
    public async Task Handle_WhenProfileDoesNotExist_ReturnsNotFoundFailure()
    {
        var userId = _fixture.Create<Guid>();
        _readRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfileDto?)null);

        var result = await _handler.Handle(new GetUserProfileQuery(userId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.ErrorMessage.Should().Be($"User profile '{userId}' was not found.");
    }
}
