using FluentValidation;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Queries.CatchUpConversationMessages;

public sealed class CatchUpConversationMessagesQueryValidator
    : AbstractValidator<CatchUpConversationMessagesQuery>
{
    public const int MaxLimit = 100;

    public CatchUpConversationMessagesQueryValidator()
    {
        RuleFor(query => query.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId is required.");

        RuleFor(query => query.RequestingUserId)
            .NotEmpty()
            .WithMessage("RequestingUserId is required.");

        RuleFor(query => query.Limit)
            .InclusiveBetween(1, MaxLimit)
            .WithMessage($"Limit must be between 1 and {MaxLimit}.");

        RuleFor(query => query.AfterSequenceNum)
            .GreaterThanOrEqualTo(0);

        RuleFor(query => query)
            .Must(query =>
                !query.ThroughSequenceNum.HasValue ||
                query.ThroughSequenceNum.Value >= query.AfterSequenceNum)
            .WithMessage("ThroughSequenceNum must be greater than or equal to AfterSequenceNum.");
    }
}
