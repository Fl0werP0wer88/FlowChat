import type { ConversationParticipant } from "../getConversationParticipants/ConversationParticipant";

export interface CopyDuetAsGroupResult {
  conversationId: string;
  name: string;
  participants: ConversationParticipant[];
}
