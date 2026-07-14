import type { ConversationMessageDto } from "./ConversationMessageDto";

export interface GetConversationMessagesResponseDto {
  items?: ConversationMessageDto[];
  nextBeforeSentAtUtc?: string | null;
  nextBeforeMessageId?: string | null;
  hasMore?: boolean;
}
