using FluentValidation;

namespace FlowChat.SocialGraphService.Application.Features.UserProfile.Queries.SearchUserProfileProjections;

public sealed class SearchUserProfileProjectionsQueryValidator : AbstractValidator<SearchUserProfileProjectionsQuery>
{
    public SearchUserProfileProjectionsQueryValidator()
    {
        RuleFor(query => query.FirstName)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Query does not contain valid FirstName.")
            .MaximumLength(100)
            .WithMessage("Query FirstName cannot be longer than 100 characters.");

        RuleFor(query => query.LastName)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Query does not contain valid LastName.")
            .MaximumLength(100)
            .WithMessage("Query LastName cannot be longer than 100 characters.");

        RuleFor(query => query.Organization)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Query does not contain valid Organization.")
            .MaximumLength(200)
            .WithMessage("Query Organization cannot be longer than 200 characters.");
    }
}
