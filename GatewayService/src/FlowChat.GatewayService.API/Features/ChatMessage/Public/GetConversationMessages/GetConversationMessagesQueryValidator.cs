using FluentValidation;

namespace FlowChat.GatewayService.Api.Features.ChatMessage.Public.GetConversationMessages;

public sealed class GetConversationMessagesQueryValidator : AbstractValidator<GetConversationMessagesQuery>
{
    public GetConversationMessagesQueryValidator()
    {
        RuleFor(query => query.ConversationId).NotEmpty();
        RuleFor(query => query.RequestingUserId).NotEmpty();
        RuleFor(query => query.Limit).InclusiveBetween(1, 100);
        RuleFor(query => query.BeforeSequenceNum)
            .GreaterThanOrEqualTo(1)
            .When(query => query.BeforeSequenceNum.HasValue);
    }
}
