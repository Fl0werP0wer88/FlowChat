using FluentValidation;

namespace FlowChat.GatewayService.Api.Features.ChatMessage.Public.CatchUpConversationMessages;

public sealed class CatchUpConversationMessagesQueryValidator
    : AbstractValidator<CatchUpConversationMessagesQuery>
{
    public CatchUpConversationMessagesQueryValidator()
    {
        RuleFor(query => query.ConversationId).NotEmpty();
        RuleFor(query => query.RequestingUserId).NotEmpty();
        RuleFor(query => query.Limit).InclusiveBetween(1, 100);
        RuleFor(query => query.AfterSequenceNum).GreaterThanOrEqualTo(0);
        RuleFor(query => query)
            .Must(query =>
                !query.ThroughSequenceNum.HasValue ||
                query.ThroughSequenceNum.Value >= query.AfterSequenceNum)
            .WithMessage("ThroughSequenceNum must be greater than or equal to AfterSequenceNum.");
    }
}
