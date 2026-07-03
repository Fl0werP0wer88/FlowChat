export interface GroupConversation {
  conversationId: string;
  name: string;
  participantCount: number;
  lastReadMsgSeqNum: number;
  currentMsgSeqNum: number;
  unreadCount: number;
}
