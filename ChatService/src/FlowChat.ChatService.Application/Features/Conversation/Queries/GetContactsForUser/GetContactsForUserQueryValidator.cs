using FluentValidation;

namespace FlowChat.ChatService.Application.Features.Conversation.Queries.GetContactsForUser;

public sealed class GetContactsForUserQueryValidator : AbstractValidator<GetContactsForUserQuery>
{
    public GetContactsForUserQueryValidator()
    {
        RuleFor(q => q.RequestingUserId)
            .NotEmpty()
            .WithMessage("RequestingUserId is required.");
    }
}
