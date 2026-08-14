import type { ConversationMessageDto } from "../getConversationMessages/ConversationMessageDto";

export interface GetGroupConversationMessagesResponseDto {
  items?: ConversationMessageDto[];
  nextBeforeSequenceNum?: number | null;
  currentSequenceNum: number;
  hasMore: boolean;
}
