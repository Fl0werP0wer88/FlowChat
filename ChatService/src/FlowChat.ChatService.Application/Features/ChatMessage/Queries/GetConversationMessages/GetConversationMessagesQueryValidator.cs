using FluentValidation;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Queries.GetConversationMessages;

public sealed class GetConversationMessagesQueryValidator : AbstractValidator<GetConversationMessagesQuery>
{
    public const int MaxLimit = 100;

    public GetConversationMessagesQueryValidator()
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

        RuleFor(query => query.BeforeSequenceNum)
            .GreaterThanOrEqualTo(1)
            .When(query => query.BeforeSequenceNum.HasValue);

        RuleFor(query => query.AfterSequenceNum)
            .GreaterThanOrEqualTo(0)
            .When(query => query.AfterSequenceNum.HasValue);

        RuleFor(query => query)
            .Must(query => !(query.BeforeSequenceNum.HasValue && query.AfterSequenceNum.HasValue))
            .WithMessage("BeforeSequenceNum and AfterSequenceNum are mutually exclusive.");

        RuleFor(query => query)
            .Must(query => !query.ThroughSequenceNum.HasValue || query.AfterSequenceNum.HasValue)
            .WithMessage("ThroughSequenceNum requires AfterSequenceNum.");

        RuleFor(query => query)
            .Must(query =>
                !query.ThroughSequenceNum.HasValue ||
                !query.AfterSequenceNum.HasValue ||
                query.ThroughSequenceNum.Value >= query.AfterSequenceNum.Value)
            .WithMessage("ThroughSequenceNum must be greater than or equal to AfterSequenceNum.");
    }
}
