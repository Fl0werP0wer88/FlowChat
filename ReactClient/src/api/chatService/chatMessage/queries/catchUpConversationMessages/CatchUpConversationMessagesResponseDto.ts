import type { ConversationMessageDto } from "../getConversationMessages/ConversationMessageDto";

export interface CatchUpConversationMessagesResponseDto {
  items?: ConversationMessageDto[];
  nextAfterSequenceNum?: number | null;
  currentSequenceNum: number;
  throughSequenceNum: number;
  hasMore: boolean;
}
