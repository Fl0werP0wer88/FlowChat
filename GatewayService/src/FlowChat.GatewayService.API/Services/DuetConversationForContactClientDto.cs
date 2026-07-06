namespace FlowChat.GatewayService.Api.Services;

public sealed record DuetConversationForContactClientDto(
    Guid PartnerUserId,
    Guid ConversationId,
    long LastReadMsgSeqNum,
    long CurrentMsgSeqNum);
