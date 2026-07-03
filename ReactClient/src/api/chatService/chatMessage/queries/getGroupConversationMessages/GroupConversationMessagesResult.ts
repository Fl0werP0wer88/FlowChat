import type { GroupConversationMessage } from "./GroupConversationMessage";

export interface GroupConversationMessagesResult {
  messages: GroupConversationMessage[];
  nextBeforeSentAtUtc: string | null;
  nextBeforeMessageId: string | null;
  hasMore: boolean;
}
