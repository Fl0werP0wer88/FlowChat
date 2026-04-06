using FluentValidation;

namespace FlowChat.SocialGraphService.Application.Features.UserProfile.Queries.SearchUserProfileProjections;

public sealed class SearchUserProfileProjectionsQueryValidator : AbstractValidator<SearchUserProfileProjectionsQuery>
{
    public SearchUserProfileProjectionsQueryValidator()
    {
        RuleFor(query => query.FirstName)
            .MaximumLength(100)
            .When(query => !string.IsNullOrWhiteSpace(query.FirstName))
            .WithMessage("Query FirstName cannot be longer than 100 characters.");

        RuleFor(query => query.LastName)
            .MaximumLength(100)
            .When(query => !string.IsNullOrWhiteSpace(query.LastName))
            .WithMessage("Query LastName cannot be longer than 100 characters.");

        RuleFor(query => query.Organization)
            .MaximumLength(200)
            .When(query => !string.IsNullOrWhiteSpace(query.Organization))
            .WithMessage("Query Organization cannot be longer than 200 characters.");

        RuleFor(query => query)
            .Must(HasAtLeastOneCriterion)
            .WithMessage("Query must contain at least one search criterion.");
    }

    private static bool HasAtLeastOneCriterion(SearchUserProfileProjectionsQuery query) =>
        !string.IsNullOrWhiteSpace(query.FirstName) ||
        !string.IsNullOrWhiteSpace(query.LastName) ||
        !string.IsNullOrWhiteSpace(query.Organization);
}
