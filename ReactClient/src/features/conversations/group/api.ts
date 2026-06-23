import { getJson, putJson } from "../../../api/httpClient";

interface OpenGroupConversationPayload {
  conversationId: string;
}

interface SendGroupChatMessagePayload {
  id: string;
  conversationId: string;
  senderDisplayName: string;
  text: string;
}

interface ConversationParticipantDto {
  userId?: string;
  UserId?: string;
  displayName?: string | null;
  DisplayName?: string | null;
  avatarUrl?: string | null;
  AvatarUrl?: string | null;
  participantUserId?: string;
  ParticipantUserId?: string;
}

interface ConversationMessageDto {
  id?: string;
  Id?: string;
  conversationId?: string;
  ConversationId?: string;
  senderUserId?: string;
  SenderUserId?: string;
  senderDisplayName?: string;
  SenderDisplayName?: string;
  text?: string;
  Text?: string;
  sentAtUtc?: string;
  SentAtUtc?: string;
}

interface OpenGroupConversationResponseDto {
  conversationId?: string;
  ConversationId?: string;
  name?: string;
  Name?: string;
  participants?: ConversationParticipantDto[];
  Participants?: ConversationParticipantDto[];
  messages?: ConversationMessageDto[];
  Messages?: ConversationMessageDto[];
  nextBeforeSentAtUtc?: string | null;
  NextBeforeSentAtUtc?: string | null;
  nextBeforeMessageId?: string | null;
  NextBeforeMessageId?: string | null;
  hasMore?: boolean;
  HasMore?: boolean;
}

interface GetGroupConversationMessagesResponseDto {
  items?: ConversationMessageDto[];
  Items?: ConversationMessageDto[];
  nextBeforeSentAtUtc?: string | null;
  NextBeforeSentAtUtc?: string | null;
  nextBeforeMessageId?: string | null;
  NextBeforeMessageId?: string | null;
  hasMore?: boolean;
  HasMore?: boolean;
}

interface SendGroupChatMessageResponseDto {
  messageId?: string;
  MessageId?: string;
  sentAtUtc?: string;
  SentAtUtc?: string;
}

export interface GroupConversationParticipant {
  userId: string;
  displayName: string | null;
  avatarUrl: string | null;
  participantUserId: string;
}

export interface GroupConversationMessage {
  id: string;
  conversationId: string;
  senderUserId: string;
  senderDisplayName: string;
  text: string;
  sentAtUtc: string;
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

export interface GroupConversationMessagesResult {
  messages: GroupConversationMessage[];
  nextBeforeSentAtUtc: string | null;
  nextBeforeMessageId: string | null;
  hasMore: boolean;
}

export interface SendGroupChatMessageResult {
  messageId: string;
  sentAtUtc: string;
}

function mapParticipant(dto: ConversationParticipantDto): GroupConversationParticipant {
  return {
    userId: dto.userId ?? dto.UserId ?? "",
    displayName: dto.displayName ?? dto.DisplayName ?? null,
    avatarUrl: dto.avatarUrl ?? dto.AvatarUrl ?? null,
    participantUserId: dto.participantUserId ?? dto.ParticipantUserId ?? "",
  };
}

function mapMessage(dto: ConversationMessageDto): GroupConversationMessage {
  return {
    id: dto.id ?? dto.Id ?? crypto.randomUUID(),
    conversationId: dto.conversationId ?? dto.ConversationId ?? "",
    senderUserId: dto.senderUserId ?? dto.SenderUserId ?? "",
    senderDisplayName: dto.senderDisplayName ?? dto.SenderDisplayName ?? "",
    text: dto.text ?? dto.Text ?? "",
    sentAtUtc: dto.sentAtUtc ?? dto.SentAtUtc ?? new Date().toISOString(),
  };
}

export async function openGroupConversation(
  conversationId: string,
  accessToken: string,
  signal?: AbortSignal,
): Promise<OpenGroupConversationResult> {
  const response = await putJson<OpenGroupConversationResponseDto, OpenGroupConversationPayload>(
    "/api/aggregate/conversations/group/open",
    { conversationId },
    {
      accessToken,
      signal,
    },
  );

  return {
    conversationId: response.conversationId ?? response.ConversationId ?? conversationId,
    name: response.name ?? response.Name ?? "",
    participants: (response.participants ?? response.Participants ?? []).map(mapParticipant),
    messages: (response.messages ?? response.Messages ?? []).map(mapMessage),
    nextBeforeSentAtUtc: response.nextBeforeSentAtUtc ?? response.NextBeforeSentAtUtc ?? null,
    nextBeforeMessageId: response.nextBeforeMessageId ?? response.NextBeforeMessageId ?? null,
    hasMore: response.hasMore ?? response.HasMore ?? false,
  };
}

export async function getGroupConversationMessages(
  conversationId: string,
  cursor: { beforeSentAtUtc: string | null; beforeMessageId: string | null },
  accessToken: string,
  signal?: AbortSignal,
): Promise<GroupConversationMessagesResult> {
  const params = new URLSearchParams({ limit: "10" });

  if (cursor.beforeSentAtUtc && cursor.beforeMessageId) {
    params.set("beforeSentAtUtc", cursor.beforeSentAtUtc);
    params.set("beforeMessageId", cursor.beforeMessageId);
  }

  const response = await getJson<GetGroupConversationMessagesResponseDto>(
    `/api/chat/conversations/${encodeURIComponent(conversationId)}/messages?${params.toString()}`,
    {
      accessToken,
      signal,
    },
  );

  return {
    messages: (response.items ?? response.Items ?? []).map(mapMessage),
    nextBeforeSentAtUtc: response.nextBeforeSentAtUtc ?? response.NextBeforeSentAtUtc ?? null,
    nextBeforeMessageId: response.nextBeforeMessageId ?? response.NextBeforeMessageId ?? null,
    hasMore: response.hasMore ?? response.HasMore ?? false,
  };
}

export async function sendGroupChatMessage(
  payload: SendGroupChatMessagePayload,
  accessToken: string,
  signal?: AbortSignal,
): Promise<SendGroupChatMessageResult> {
  const response = await putJson<SendGroupChatMessageResponseDto, SendGroupChatMessagePayload>(
    "/api/chat/messages",
    payload,
    {
      accessToken,
      signal,
    },
  );

  return {
    messageId: response.messageId ?? response.MessageId ?? payload.id,
    sentAtUtc: response.sentAtUtc ?? response.SentAtUtc ?? new Date().toISOString(),
  };
}
