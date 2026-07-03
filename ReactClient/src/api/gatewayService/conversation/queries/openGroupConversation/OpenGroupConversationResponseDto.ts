import type { ConversationMessageDto } from "../getConversationMessages/ConversationMessageDto";
import type { ConversationParticipantDto } from "../getConversationParticipants/ConversationParticipantDto";

export interface OpenGroupConversationResponseDto {
  conversationId?: string;
  name?: string;
  participants?: ConversationParticipantDto[];
  messages?: ConversationMessageDto[];
  nextBeforeSentAtUtc?: string | null;
  nextBeforeMessageId?: string | null;
  hasMore?: boolean;
}
