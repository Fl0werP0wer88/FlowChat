import type { ConversationParticipantDto } from "../getConversationParticipants/ConversationParticipantDto";

export interface CopyDuetAsGroupResponseDto {
  conversationId?: string;
  name?: string;
  participants?: ConversationParticipantDto[];
}
