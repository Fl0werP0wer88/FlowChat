using FlowChat.Shared.Domain.ValueObjects;
using FluentValidation;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.SearchUserProfilesRangeAscending;

public sealed class SearchUserProfilesRangeAscendingQueryValidator
    : AbstractValidator<SearchUserProfilesRangeAscendingQuery>
{
    public const int MaxLimit = 100;

    public SearchUserProfilesRangeAscendingQueryValidator()
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

        RuleFor(query => query.Cursor)
            .Must(cursor => FriendlyUserId.TryCreate(cursor, out _))
            .When(query => !string.IsNullOrWhiteSpace(query.Cursor))
            .WithMessage("Query Cursor must be a valid FriendlyUserId.");

        RuleFor(query => query.Limit)
            .InclusiveBetween(1, MaxLimit);

        RuleFor(query => query)
            .Must(HasAtLeastOneCriterion)
            .WithMessage("Query must contain at least one search criterion.");
    }

    private static bool HasAtLeastOneCriterion(SearchUserProfilesRangeAscendingQuery query) =>
        !string.IsNullOrWhiteSpace(query.FirstName) ||
        !string.IsNullOrWhiteSpace(query.LastName) ||
        !string.IsNullOrWhiteSpace(query.Organization);
}
