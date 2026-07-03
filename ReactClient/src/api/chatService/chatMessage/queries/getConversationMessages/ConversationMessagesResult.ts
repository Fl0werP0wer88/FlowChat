import type { ConversationMessage } from "./ConversationMessage";

export interface ConversationMessagesResult {
  messages: ConversationMessage[];
  nextBeforeSentAtUtc: string | null;
  nextBeforeMessageId: string | null;
  hasMore: boolean;
}
