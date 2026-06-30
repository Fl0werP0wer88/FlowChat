import type { ChatMessage, MessageSender } from "../../types/chat";
import type { GroupConversationMessage } from "../../api/chatApi";

export interface GroupConversationCacheEntry {
  conversationId: string;
  name: string;
  messages: ChatMessage[];
  nextBeforeSentAtUtc: string | null;
  nextBeforeMessageId: string | null;
  hasMore: boolean;
}

export function createGroupMessage(
  sender: MessageSender,
  text: string,
  sentAtUtc = new Date().toISOString(),
  id: string = crypto.randomUUID(),
  conversationId: string | null = null,
  senderUserId: string | null = null,
  senderDisplayName: string | null = null,
): ChatMessage {
  return { id, conversationId, senderUserId, senderDisplayName, sender, text, sentAtUtc };
}

export function sortGroupMessages(messages: ChatMessage[]): ChatMessage[] {
  return [...messages].sort((a, b) => a.sentAtUtc.localeCompare(b.sentAtUtc));
}

export function mapGroupConversationMessage(
  message: GroupConversationMessage,
  ownerUserId: string | null,
): ChatMessage {
  const sender = ownerUserId && message.senderUserId === ownerUserId ? "me" : "other";
  return createGroupMessage(
    sender,
    message.text,
    message.sentAtUtc,
    message.id,
    message.conversationId,
    message.senderUserId,
    message.senderDisplayName,
  );
}
