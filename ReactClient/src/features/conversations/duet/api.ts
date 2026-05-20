import { getJson, postJson, putJson } from "../../../api/httpClient";

interface OpenDuetConversationPayload {
  partnerUserId: string;
  knownConversationId: string | null;
}

interface SendChatMessagePayload {
  id: string;
  conversationId: string;
  senderDisplayName: string;
  text: string;
}

interface CopyDuetAsGroupPayload {
  newGroupConversationId: string;
  partnerUserId: string;
}

interface CopyDuetAsGroupResponseDto {
  conversationId?: string;
  ConversationId?: string;
  name?: string;
  Name?: string;
  participants?: ConversationParticipantDto[];
  Participants?: ConversationParticipantDto[];
}

interface SendChatMessageResponseDto {
  messageId?: string;
  MessageId?: string;
  sentAtUtc?: string;
  SentAtUtc?: string;
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

interface OpenDuetConversationResponseDto {
  conversationId?: string;
  ConversationId?: string;
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

export interface ConversationParticipant {
  userId: string;
  displayName: string | null;
  avatarUrl: string | null;
  participantUserId: string;
}

export interface ConversationMessage {
  id: string;
  conversationId: string;
  senderUserId: string;
  senderDisplayName: string;
  text: string;
  sentAtUtc: string;
}

export interface OpenDuetConversationResult {
  conversationId: string;
  participants: ConversationParticipant[];
  messages: ConversationMessage[];
  nextBeforeSentAtUtc: string | null;
  nextBeforeMessageId: string | null;
  hasMore: boolean;
}

export interface SendChatMessageResult {
  messageId: string;
  sentAtUtc: string;
}

export interface CopyDuetAsGroupResult {
  conversationId: string;
  name: string;
  participants: ConversationParticipant[];
}

interface GetConversationMessagesResponseDto {
  items?: ConversationMessageDto[];
  Items?: ConversationMessageDto[];
  nextBeforeSentAtUtc?: string | null;
  NextBeforeSentAtUtc?: string | null;
  nextBeforeMessageId?: string | null;
  NextBeforeMessageId?: string | null;
  hasMore?: boolean;
  HasMore?: boolean;
}

export interface ConversationMessagesResult {
  messages: ConversationMessage[];
  nextBeforeSentAtUtc: string | null;
  nextBeforeMessageId: string | null;
  hasMore: boolean;
}

function mapParticipant(dto: ConversationParticipantDto): ConversationParticipant {
  return {
    userId: dto.userId ?? dto.UserId ?? "",
    displayName: dto.displayName ?? dto.DisplayName ?? null,
    avatarUrl: dto.avatarUrl ?? dto.AvatarUrl ?? null,
    participantUserId: dto.participantUserId ?? dto.ParticipantUserId ?? "",
  };
}

function mapMessage(dto: ConversationMessageDto): ConversationMessage {
  return {
    id: dto.id ?? dto.Id ?? crypto.randomUUID(),
    conversationId: dto.conversationId ?? dto.ConversationId ?? "",
    senderUserId: dto.senderUserId ?? dto.SenderUserId ?? "",
    senderDisplayName: dto.senderDisplayName ?? dto.SenderDisplayName ?? "",
    text: dto.text ?? dto.Text ?? "",
    sentAtUtc: dto.sentAtUtc ?? dto.SentAtUtc ?? new Date().toISOString(),
  };
}

export async function openDuetConversation(
  partnerUserId: string,
  knownConversationId: string | null,
  accessToken: string,
  signal?: AbortSignal,
): Promise<OpenDuetConversationResult> {
  const response = await putJson<OpenDuetConversationResponseDto, OpenDuetConversationPayload>(
    "/api/aggregate/conversations/duet/open",
    {
      partnerUserId,
      knownConversationId,
    },
    {
      accessToken,
      signal,
    },
  );

  return {
    conversationId: response.conversationId ?? response.ConversationId ?? "",
    participants: (response.participants ?? response.Participants ?? []).map(mapParticipant),
    messages: (response.messages ?? response.Messages ?? []).map(mapMessage),
    nextBeforeSentAtUtc: response.nextBeforeSentAtUtc ?? response.NextBeforeSentAtUtc ?? null,
    nextBeforeMessageId: response.nextBeforeMessageId ?? response.NextBeforeMessageId ?? null,
    hasMore: response.hasMore ?? response.HasMore ?? false,
  };
}

export async function getConversationMessages(
  conversationId: string,
  cursor: { beforeSentAtUtc: string | null; beforeMessageId: string | null },
  accessToken: string,
  signal?: AbortSignal,
): Promise<ConversationMessagesResult> {
  const params = new URLSearchParams({ limit: "10" });

  if (cursor.beforeSentAtUtc && cursor.beforeMessageId) {
    params.set("beforeSentAtUtc", cursor.beforeSentAtUtc);
    params.set("beforeMessageId", cursor.beforeMessageId);
  }

  const response = await getJson<GetConversationMessagesResponseDto>(
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

export async function sendChatMessage(
  payload: SendChatMessagePayload,
  accessToken: string,
  signal?: AbortSignal,
): Promise<SendChatMessageResult> {
  const response = await putJson<SendChatMessageResponseDto, SendChatMessagePayload>(
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

export async function copyDuetAsGroup(
  partnerUserId: string,
  accessToken: string,
  signal?: AbortSignal,
): Promise<CopyDuetAsGroupResult> {
  const newGroupConversationId = crypto.randomUUID();
  const response = await postJson<CopyDuetAsGroupResponseDto, CopyDuetAsGroupPayload>(
    "/api/conversations/duet/copy-as-group",
    {
      newGroupConversationId,
      partnerUserId,
    },
    {
      accessToken,
      signal,
    },
  );

  return {
    conversationId: response.conversationId ?? response.ConversationId ?? newGroupConversationId,
    name: response.name ?? response.Name ?? "",
    participants: (response.participants ?? response.Participants ?? []).map(mapParticipant),
  };
}
