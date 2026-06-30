using FluentValidation;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.GetUserProfiles;

public sealed class GetUserProfilesQueryValidator : AbstractValidator<GetUserProfilesQuery>
{
    public GetUserProfilesQueryValidator()
    {
        RuleFor(query => query.UserIds)
            .Must(HasAtLeastOneUserId)
            .WithMessage("Query must contain at least one user ID.");
    }

    private static bool HasAtLeastOneUserId(IReadOnlyList<Guid>? userIds) =>
        userIds is not null && userIds.Any(userId => userId != Guid.Empty);
}
