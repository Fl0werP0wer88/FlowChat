import type { ConversationMessage } from "../getConversationMessages/ConversationMessage";
import type { ConversationParticipant } from "../getConversationParticipants/ConversationParticipant";

export interface OpenDuetConversationResult {
  conversationId: string;
  participants: ConversationParticipant[];
  messages: ConversationMessage[];
  nextBeforeSentAtUtc: string | null;
  nextBeforeMessageId: string | null;
  hasMore: boolean;
}
