import type { ConversationMessageDto } from "../getConversationMessages/ConversationMessageDto";

export interface GetGroupConversationMessagesResponseDto {
  items?: ConversationMessageDto[];
  nextBeforeSequenceNum?: number | null;
  nextAfterSequenceNum?: number | null;
  currentSequenceNum: number;
  throughSequenceNum?: number | null;
  hasMore: boolean;
}
