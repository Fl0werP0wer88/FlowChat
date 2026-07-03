import type { ConversationMessageDto } from "../getConversationMessages/ConversationMessageDto";

export interface GetGroupConversationMessagesResponseDto {
  items?: ConversationMessageDto[];
  nextBeforeSentAtUtc?: string | null;
  nextBeforeMessageId?: string | null;
  hasMore?: boolean;
}
