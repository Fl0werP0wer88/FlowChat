using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.GetUserProfiles;
using FluentAssertions;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class GetUserProfilesQueryValidatorTests
{
    private readonly GetUserProfilesQueryValidator _validator = new();

    [Fact]
    public async Task Validate_WhenQueryContainsUserId_ReturnsValid()
    {
        var result = await _validator.ValidateAsync(new GetUserProfilesQuery([Guid.NewGuid()]));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WhenQueryIsEmpty_ReturnsValidationError()
    {
        var result = await _validator.ValidateAsync(new GetUserProfilesQuery([]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == "Query must contain at least one user ID.");
    }

    [Fact]
    public async Task Validate_WhenQueryContainsOnlyEmptyIds_ReturnsValidationError()
    {
        var result = await _validator.ValidateAsync(new GetUserProfilesQuery([Guid.Empty]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == "Query must contain at least one user ID.");
    }
}
