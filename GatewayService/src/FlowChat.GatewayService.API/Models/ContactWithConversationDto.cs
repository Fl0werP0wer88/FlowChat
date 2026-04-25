using FlowChat.Core.Contracts;

namespace FlowChat.GatewayService.Api.Models;

public sealed record ContactWithConversationDto(
    Guid Id,
    Guid ContactUserId,
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Email,
    bool IsBlocked,
    Guid? ConversationId) : IServiceOutput;
