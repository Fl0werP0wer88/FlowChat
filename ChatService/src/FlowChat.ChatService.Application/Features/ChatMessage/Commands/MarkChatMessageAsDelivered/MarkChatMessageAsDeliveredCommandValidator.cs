using FlowChat.Shared.Domain.ValueObjects;
using FluentValidation;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.MarkChatMessageAsDelivered;

public sealed class MarkChatMessageAsDeliveredCommandValidator : AbstractValidator<MarkChatMessageAsDeliveredCommand>
{
    public MarkChatMessageAsDeliveredCommandValidator()
    {
        RuleFor(command => command.MessageId)
            .NotEmpty()
            .WithMessage("MessageId is required.");

        RuleFor(command => command.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId is required.");

        RuleFor(command => command.DeliveredAtUtc)
            .Must(value => value.Offset == TimeSpan.Zero)
            .WithMessage(UtcDateTimeOffset.InvalidUtcDateTimeOffsetMessage);
    }
}
