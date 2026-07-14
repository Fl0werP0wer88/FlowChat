export interface ConversationMessageDto {
  id?: string;
  conversationId?: string;
  senderUserId?: string;
  text?: string;
  sequenceNum?: number | null;
  sentAtUtc?: string;
}
