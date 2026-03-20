export type MessageSender = "me" | "other" | "system";

export interface ChatMessage {
  id: string;
  sender: MessageSender;
  text: string;
  createdAt: string;
}
