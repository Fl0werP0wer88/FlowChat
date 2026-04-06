using FlowChat.SocialGraphService.Application.Features.UserProfile.Queries.SearchUserProfileProjections;
using FluentAssertions;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class SearchUserProfileProjectionsQueryValidatorTests
{
    private readonly SearchUserProfileProjectionsQueryValidator _validator = new();

    [Fact]
    public async Task Validate_WhenQueryIsValid_ReturnsValid()
    {
        var result = await _validator.ValidateAsync(CreateValidQuery());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WhenOnlyFirstNameIsProvided_ReturnsValid()
    {
        var result = await _validator.ValidateAsync(new SearchUserProfileProjectionsQuery("Jane", null, null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WhenOnlyLastNameIsProvided_ReturnsValid()
    {
        var result = await _validator.ValidateAsync(new SearchUserProfileProjectionsQuery(null, "Doe", null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WhenOnlyOrganizationIsProvided_ReturnsValid()
    {
        var result = await _validator.ValidateAsync(new SearchUserProfileProjectionsQuery(null, null, "FlowChat"));

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
        var result = await _validator.ValidateAsync(new SearchUserProfileProjectionsQuery(firstName, lastName, organization));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == "Query must contain at least one search criterion.");
    }

    [Fact]
    public async Task Validate_WhenOrganizationIsTooLong_ReturnsValidationError()
    {
        var result = await _validator.ValidateAsync(CreateValidQuery() with { Organization = new string('a', 201) });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == "Query Organization cannot be longer than 200 characters.");
    }

    [Fact]
    public async Task Validate_WhenFirstNameIsTooLong_ReturnsValidationError()
    {
        var result = await _validator.ValidateAsync(CreateValidQuery() with { FirstName = new string('a', 101) });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == "Query FirstName cannot be longer than 100 characters.");
    }

    [Fact]
    public async Task Validate_WhenLastNameIsTooLong_ReturnsValidationError()
    {
        var result = await _validator.ValidateAsync(CreateValidQuery() with { LastName = new string('a', 101) });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == "Query LastName cannot be longer than 100 characters.");
    }

    private static SearchUserProfileProjectionsQuery CreateValidQuery() =>
        new("Jane", "Doe", "FlowChat");
}
