import type { GroupConversationMessage } from "../getConversationMessages/GroupConversationMessage";
import type { GroupConversationParticipant } from "../getConversationParticipants/GroupConversationParticipant";

export interface OpenGroupConversationResult {
  conversationId: string;
  name: string;
  participants: GroupConversationParticipant[];
  messages: GroupConversationMessage[];
  nextBeforeSentAtUtc: string | null;
  nextBeforeMessageId: string | null;
  hasMore: boolean;
}
