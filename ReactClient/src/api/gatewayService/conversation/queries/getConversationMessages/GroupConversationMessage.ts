export interface GroupConversationMessage {
  id: string;
  conversationId: string;
  senderUserId: string;
  senderDisplayName: string;
  text: string;
  sequenceNum: number | null;
  sentAtUtc: string;
}
