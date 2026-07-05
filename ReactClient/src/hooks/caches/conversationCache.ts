import type { ChatMessage, ConversationMessage, MessageSender } from "../../types/chat";

export interface ConversationCacheEntry {
  conversationId: string;
  messages: ChatMessage[];
  nextBeforeSentAtUtc: string | null;
  nextBeforeMessageId: string | null;
  hasMore: boolean;
}

export function createMessage(
  sender: MessageSender,
  text: string,
  sentAtUtc = new Date().toISOString(),
  id: string = crypto.randomUUID(),
  conversationId: string | null = null,
  senderUserId: string | null = null,
  senderDisplayName: string | null = null,
  sequenceNum: number | null = null,
): ChatMessage {
  return { id, conversationId, senderUserId, senderDisplayName, sender, text, sequenceNum, sentAtUtc };
}

export function sortMessages(messages: ChatMessage[]): ChatMessage[] {
  return [...messages].sort((a, b) => a.sentAtUtc.localeCompare(b.sentAtUtc));
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
    null,
  );
}
