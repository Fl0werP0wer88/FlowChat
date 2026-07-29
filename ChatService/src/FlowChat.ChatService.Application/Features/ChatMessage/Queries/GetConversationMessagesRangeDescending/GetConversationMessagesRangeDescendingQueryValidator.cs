using FluentValidation;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Queries.GetConversationMessagesRangeDescending;

public sealed class GetConversationMessagesRangeDescendingQueryValidator
    : AbstractValidator<GetConversationMessagesRangeDescendingQuery>
{
    public const int MaxLimit = 100;

    public GetConversationMessagesRangeDescendingQueryValidator()
    {
        RuleFor(query => query.ConversationId).NotEmpty();
        RuleFor(query => query.RequestingUserId).NotEmpty();
        RuleFor(query => query.StartSequenceNum)
            .GreaterThanOrEqualTo(1)
            .When(query => query.StartSequenceNum.HasValue);
        RuleFor(query => query.EndSequenceNum)
            .GreaterThanOrEqualTo(0)
            .When(query => query.EndSequenceNum.HasValue);
        RuleFor(query => query.Limit).InclusiveBetween(1, MaxLimit);
    }
}
