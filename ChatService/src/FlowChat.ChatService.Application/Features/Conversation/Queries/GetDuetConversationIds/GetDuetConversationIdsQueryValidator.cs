using FluentValidation;

namespace FlowChat.ChatService.Application.Features.Conversation.Queries.GetDuetConversationIds;

public sealed class GetDuetConversationIdsQueryValidator : AbstractValidator<GetDuetConversationIdsQuery>
{
    public const int MaxPartnerCount = 500;

    public GetDuetConversationIdsQueryValidator()
    {
        RuleFor(q => q.RequestingUserId)
            .NotEmpty()
            .WithMessage("RequestingUserId is required.");

        RuleFor(q => q.PartnerUserIds)
            .NotEmpty()
            .WithMessage("At least one PartnerUserId is required.")
            .Must(ids => ids.Count <= MaxPartnerCount)
            .WithMessage($"Cannot request more than {MaxPartnerCount} conversation IDs at once.");
    }
}
