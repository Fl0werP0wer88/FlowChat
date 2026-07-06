using FluentValidation;

namespace FlowChat.ChatService.Application.Features.Conversation.Queries.GetDuetConversationsForContacts;

public sealed class GetDuetConversationsForContactsQueryValidator : AbstractValidator<GetDuetConversationsForContactsQuery>
{
    public const int MaxPartnerCount = 500;

    public GetDuetConversationsForContactsQueryValidator()
    {
        RuleFor(q => q.RequestingUserId)
            .NotEmpty()
            .WithMessage("RequestingUserId is required.");

        RuleFor(q => q.PartnerUserIds)
            .NotEmpty()
            .WithMessage("At least one PartnerUserId is required.")
            .Must(ids => ids.Count <= MaxPartnerCount)
            .WithMessage($"Cannot request more than {MaxPartnerCount} contact conversations at once.");
    }
}
