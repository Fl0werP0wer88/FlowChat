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

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenFirstNameIsBlank_ReturnsValidationError(string firstName)
    {
        var result = await _validator.ValidateAsync(CreateValidQuery() with { FirstName = firstName });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == "Query does not contain valid FirstName.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenLastNameIsBlank_ReturnsValidationError(string lastName)
    {
        var result = await _validator.ValidateAsync(CreateValidQuery() with { LastName = lastName });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == "Query does not contain valid LastName.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenOrganizationIsBlank_ReturnsValidationError(string organization)
    {
        var result = await _validator.ValidateAsync(CreateValidQuery() with { Organization = organization });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == "Query does not contain valid Organization.");
    }

    [Fact]
    public async Task Validate_WhenOrganizationIsTooLong_ReturnsValidationError()
    {
        var result = await _validator.ValidateAsync(CreateValidQuery() with { Organization = new string('a', 201) });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == "Query Organization cannot be longer than 200 characters.");
    }

    private static SearchUserProfileProjectionsQuery CreateValidQuery() =>
        new("Jane", "Doe", "FlowChat");
}
