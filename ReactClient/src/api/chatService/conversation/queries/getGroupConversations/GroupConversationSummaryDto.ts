export interface GroupConversationSummaryDto {
  conversationId?: string;
  name?: string;
  participantCount?: number;
  lastReadMsgSeqNum?: number;
  currentMsgSeqNum?: number;
}
