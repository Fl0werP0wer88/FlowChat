import type { ChatMessage, DuetConversationMessage, DuetConversationParticipant, MessageSender } from "../../types/chat";
import type { MessageSyncStatus } from "./sequencedMessageCache";
import { sortSequencedMessages } from "./sequencedMessageCache";

export interface DuetConversationCacheEntry {
  conversationId: string;
  participants: DuetConversationParticipant[];
  messages: ChatMessage[];
  nextBeforeSequenceNum: number | null;
  hasMore: boolean;
  lastContiguousSequenceNum: number;
  pendingMessagesBySequence: Record<number, ChatMessage>;
  syncStatus: MessageSyncStatus;
}

export function createDuetMessage(
  sender: MessageSender,
  text: string,
  sentAtUtc = new Date().toISOString(),
  id: string = crypto.randomUUID(),
  conversationId: string | null = null,
  senderUserId: string | null = null,
  sequenceNum: number | null = null,
): ChatMessage {
  return { id, conversationId, senderUserId, sender, text, sequenceNum, sentAtUtc };
}

export function sortDuetMessages(messages: ChatMessage[]): ChatMessage[] {
  return sortSequencedMessages(messages);
}

export function mapDuetConversationMessage(
  message: DuetConversationMessage,
  ownerUserId: string | null,
): ChatMessage {
  const sender = ownerUserId && message.senderUserId === ownerUserId ? "me" : "other";
  return createDuetMessage(
    sender,
    message.text,
    message.sentAtUtc,
    message.id,
    message.conversationId,
    message.senderUserId,
    message.sequenceNum,
  );
}
