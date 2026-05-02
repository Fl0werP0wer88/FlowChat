using FluentValidation;

namespace FlowChat.PresenceService.Application.Features.Presence.Queries.GetPresenceStatusesBatch;

public sealed class GetPresenceStatusesBatchQueryValidator : AbstractValidator<GetPresenceStatusesBatchQuery>
{
    public GetPresenceStatusesBatchQueryValidator()
    {
        RuleFor(x => x.UserIds).NotNull();
        RuleForEach(x => x.UserIds).NotEmpty();
    }
}
