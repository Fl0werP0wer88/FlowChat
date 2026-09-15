using FluentValidation;

namespace FlowChat.ChatService.Application.Features.Conversation.Queries.GetDuetConversations;

public sealed class GetDuetConversationsQueryValidator : AbstractValidator<GetDuetConversationsQuery>
{
    public GetDuetConversationsQueryValidator()
    {
        RuleFor(q => q.RequestingUserId)
            .NotEmpty()
            .WithMessage("RequestingUserId is required.");
    }
}
