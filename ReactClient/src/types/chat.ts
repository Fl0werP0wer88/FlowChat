export type MessageSender = "me" | "system";

export interface ChatMessage {
  id: string;
  sender: MessageSender;
  text: string;
  createdAt: string;
}
