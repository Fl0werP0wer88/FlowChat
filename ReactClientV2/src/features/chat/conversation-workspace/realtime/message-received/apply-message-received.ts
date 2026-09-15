import type { ConversationMessage } from '../../api/conversation-contracts';
import {
  mergeConversationMessage,
  type ConversationMessageBufferState,
  type MessageMergeResult,
} from '../../cache/conversation-message-buffer';

export function applyMessageReceived<TState extends ConversationMessageBufferState>(
  current: TState | undefined,
  message: ConversationMessage,
): MessageMergeResult<TState> | null {
  if (!current) return null;

  return mergeConversationMessage(current, message);
}
