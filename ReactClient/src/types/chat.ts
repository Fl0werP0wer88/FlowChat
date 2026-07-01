export type MessageSender = "me" | "other" | "system";

export interface ChatMessage {
  id: string;
  conversationId: string | null;
  senderUserId: string | null;
  senderDisplayName: string | null;
  sender: MessageSender;
  text: string;
  sequenceNum: number | null;
  sentAtUtc: string;
}
