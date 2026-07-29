import type { ConversationMessageDto } from "./ConversationMessageDto";

export interface GetConversationMessagesResponseDto {
  items?: ConversationMessageDto[];
  nextBeforeSequenceNum?: number | null;
  nextAfterSequenceNum?: number | null;
  currentSequenceNum: number;
  throughSequenceNum?: number | null;
  hasMore: boolean;
}
