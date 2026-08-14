import type { ConversationMessageDto } from "../getConversationMessages/ConversationMessageDto";
import type { ConversationParticipantDto } from "../getConversationParticipants/ConversationParticipantDto";

export interface OpenGroupConversationResponseDto {
  conversationId?: string;
  name?: string;
  participants?: ConversationParticipantDto[];
  messages?: ConversationMessageDto[];
  nextBeforeSequenceNum?: number | null;
  currentSequenceNum: number;
  hasMore: boolean;
}
