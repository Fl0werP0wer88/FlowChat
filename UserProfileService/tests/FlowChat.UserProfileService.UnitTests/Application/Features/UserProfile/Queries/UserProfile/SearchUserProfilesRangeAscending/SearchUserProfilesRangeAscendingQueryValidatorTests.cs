using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.SearchUserProfilesRangeAscending;
using FluentAssertions;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class SearchUserProfilesRangeAscendingQueryValidatorTests
{
    private readonly SearchUserProfilesRangeAscendingQueryValidator _validator = new();

    [Fact]
    public async Task Validate_WhenQueryIsValid_ReturnsValid()
    {
        var result = await _validator.ValidateAsync(CreateValidQuery());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, null, null)]
    [InlineData("", "", "")]
    [InlineData("   ", null, "  ")]
    public async Task Validate_WhenNoCriteriaProvided_ReturnsValidationError(
        string? firstName,
        string? lastName,
        string? organization)
    {
        var result = await _validator.ValidateAsync(
            CreateValidQuery() with
            {
                FirstName = firstName,
                LastName = lastName,
                Organization = organization
            });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "Query must contain at least one search criterion.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Validate_WhenLimitIsOutsideAllowedRange_ReturnsValidationError(int limit)
    {
        var result = await _validator.ValidateAsync(CreateValidQuery() with { Limit = limit });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Limit");
    }

    [Fact]
    public async Task Validate_WhenCursorIsInvalid_ReturnsValidationError()
    {
        var result = await _validator.ValidateAsync(CreateValidQuery() with { Cursor = "not a cursor!" });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.ErrorMessage == "Query Cursor must be a valid FriendlyUserId.");
    }

    [Theory]
    [InlineData(nameof(SearchUserProfilesRangeAscendingQuery.FirstName), 101)]
    [InlineData(nameof(SearchUserProfilesRangeAscendingQuery.LastName), 101)]
    [InlineData(nameof(SearchUserProfilesRangeAscendingQuery.Organization), 201)]
    public async Task Validate_WhenCriterionIsTooLong_ReturnsValidationError(
        string propertyName,
        int length)
    {
        var query = propertyName switch
        {
            nameof(SearchUserProfilesRangeAscendingQuery.FirstName) =>
                CreateValidQuery() with { FirstName = new string('a', length) },
            nameof(SearchUserProfilesRangeAscendingQuery.LastName) =>
                CreateValidQuery() with { LastName = new string('a', length) },
            _ => CreateValidQuery() with { Organization = new string('a', length) }
        };

        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == propertyName);
    }

    private static SearchUserProfilesRangeAscendingQuery CreateValidQuery() =>
        new("Jane", null, null, "jdoe", 20);
}
