import type { ChatMessage, MessageSender } from "../../../types/chat";
import type { ConversationMessage } from "../api";

export interface ConversationCacheEntry {
  conversationId: string;
  messages: ChatMessage[];
}

export function createMessage(
  sender: MessageSender,
  text: string,
  createdAt = new Date().toISOString(),
  id: string = crypto.randomUUID(),
  conversationId: string | null = null,
  senderUserId: string | null = null,
  senderDisplayName: string | null = null,
): ChatMessage {
  return { id, conversationId, senderUserId, senderDisplayName, sender, text, createdAt };
}

export function mapConversationMessage(
  message: ConversationMessage,
  ownerUserId: string | null,
): ChatMessage {
  const sender = ownerUserId && message.senderUserId === ownerUserId ? "me" : "other";
  return createMessage(
    sender,
    message.text,
    message.sentAtUtc,
    message.id,
    message.conversationId,
    message.senderUserId,
    message.senderDisplayName,
  );
}
