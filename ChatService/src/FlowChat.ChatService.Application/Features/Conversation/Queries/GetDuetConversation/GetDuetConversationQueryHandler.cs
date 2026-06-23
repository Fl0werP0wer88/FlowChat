using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Application.Features.Conversation.Queries.GetDuetConversation;

public sealed class GetDuetConversationQueryHandler : IQueryHandler<GetDuetConversationQuery, DuetConversationDetailDto>
{
    private readonly IDuetConversationReadRepository _duetConversationReadRepository;

    public GetDuetConversationQueryHandler(IDuetConversationReadRepository duetConversationReadRepository)
    {
        _duetConversationReadRepository = duetConversationReadRepository ?? throw new ArgumentNullException(nameof(duetConversationReadRepository));
    }

    public async Task<FlowChatResult<DuetConversationDetailDto>> Handle(
        GetDuetConversationQuery request,
        CancellationToken cancellationToken)
    {
        var conversation = await _duetConversationReadRepository.GetByUserIdsAsync(
            request.RequestingUserId,
            request.PartnerUserId,
            cancellationToken);

        return conversation is not null
            ? FlowChatResult<DuetConversationDetailDto>.Success(conversation)
            : FlowChatResult<DuetConversationDetailDto>.Failure(
                DomainError.NotFound("No duet conversation found between the specified users."));
    }
}
