export type MessageSender = "me" | "other" | "system";

export interface ChatMessage {
  id: string;
  conversationId: string | null;
  senderUserId: string | null;
  senderDisplayName: string | null;
  sender: MessageSender;
  text: string;
  sequenceNum: number | null;
  sentAtUtc: string;
}

export interface ConversationMessage {
  id: string;
  conversationId: string;
  senderUserId: string;
  senderDisplayName: string;
  text: string;
  sequenceNum: number | null;
  sentAtUtc: string;
}

export interface GroupConversationMessage {
  id: string;
  conversationId: string;
  senderUserId: string;
  senderDisplayName: string;
  text: string;
  sequenceNum: number | null;
  sentAtUtc: string;
}

export interface ConversationParticipant {
  userId: string;
  displayName: string | null;
  avatarUrl: string | null;
  participantUserId: string;
}

export interface GroupConversationParticipant {
  userId: string;
  displayName: string | null;
  avatarUrl: string | null;
  participantUserId: string;
}

export interface GroupConversation {
  conversationId: string;
  name: string;
  participantCount: number;
  lastReadMsgSeqNum: number;
  currentMsgSeqNum: number;
  unreadCount: number;
}

export interface ConversationMessagesResult {
  messages: ConversationMessage[];
  nextBeforeSentAtUtc: string | null;
  nextBeforeMessageId: string | null;
  hasMore: boolean;
}

export interface GroupConversationMessagesResult {
  messages: GroupConversationMessage[];
  nextBeforeSentAtUtc: string | null;
  nextBeforeMessageId: string | null;
  hasMore: boolean;
}

export interface SendChatMessageResult {
  messageId: string;
  sentAtUtc: string;
}

export interface SendGroupChatMessageResult {
  messageId: string;
  sentAtUtc: string;
}

export interface CopyDuetAsGroupResult {
  conversationId: string;
  name: string;
  participants: ConversationParticipant[];
}

export interface OpenDuetConversationResult {
  conversationId: string;
  participants: ConversationParticipant[];
  messages: ConversationMessage[];
  nextBeforeSentAtUtc: string | null;
  nextBeforeMessageId: string | null;
  hasMore: boolean;
}

export interface OpenGroupConversationResult {
  conversationId: string;
  name: string;
  participants: GroupConversationParticipant[];
  messages: GroupConversationMessage[];
  nextBeforeSentAtUtc: string | null;
  nextBeforeMessageId: string | null;
  hasMore: boolean;
}
