import type { ConversationMessageDto } from "./ConversationMessageDto";

export interface GetConversationMessagesResponseDto {
  items?: ConversationMessageDto[];
  nextBeforeSequenceNum?: number | null;
  currentSequenceNum: number;
  hasMore: boolean;
}
