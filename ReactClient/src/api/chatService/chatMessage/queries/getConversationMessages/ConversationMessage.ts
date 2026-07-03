export interface ConversationMessage {
  id: string;
  conversationId: string;
  senderUserId: string;
  senderDisplayName: string;
  text: string;
  sequenceNum: number | null;
  sentAtUtc: string;
}
