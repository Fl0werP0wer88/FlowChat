using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.ConfirmEmailVerification;

public sealed record ConfirmEmailVerificationCommand(string Token) : ICommand<IdempotentCommandResult<Unit>>;
