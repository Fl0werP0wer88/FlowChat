import type { ChatMessage, GroupConversationMessage, GroupConversationParticipant, MessageSender } from "../../types/chat";
import type { MessageSyncStatus } from "./sequencedMessageCache";
import { sortSequencedMessages } from "./sequencedMessageCache";

export interface GroupConversationCacheEntry {
  conversationId: string;
  name: string;
  participants: GroupConversationParticipant[];
  messages: ChatMessage[];
  nextBeforeSequenceNum: number | null;
  hasMore: boolean;
  lastContiguousSequenceNum: number;
  pendingMessagesBySequence: Record<number, ChatMessage>;
  syncStatus: MessageSyncStatus;
}

export function createGroupMessage(
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

export function sortGroupMessages(messages: ChatMessage[]): ChatMessage[] {
  return sortSequencedMessages(messages);
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
    message.sequenceNum,
  );
}
