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

        RuleFor(query => query.BeforeSentAtUtc)
            .Must(value => value is null || value.Value.Offset == TimeSpan.Zero)
            .WithMessage("BeforeSentAtUtc must be in UTC.");

        RuleFor(query => query.BeforeMessageId)
            .NotEmpty()
            .When(query => query.BeforeMessageId.HasValue)
            .WithMessage("BeforeMessageId cannot be empty.");

        RuleFor(query => query.BeforeSentAtUtc)
            .NotNull()
            .When(query => query.BeforeMessageId.HasValue)
            .WithMessage("BeforeSentAtUtc is required when BeforeMessageId is provided.");
    }
}
