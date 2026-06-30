import { getJson, postJson, putJson } from "./httpClient";

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

interface GroupConversationSummaryDto {
  conversationId?: string;
  ConversationId?: string;
  name?: string;
  Name?: string;
  participantCount?: number;
  ParticipantCount?: number;
}

interface GetGroupConversationsResponseDto {
  groupConversations?: GroupConversationSummaryDto[];
  GroupConversations?: GroupConversationSummaryDto[];
}

interface CreateGroupConversationRequestDto {
  conversationId: string;
  participantUserIds: string[];
  name: string;
}

interface CreateGroupConversationResponseDto {
  conversationId?: string;
  ConversationId?: string;
  name?: string;
  Name?: string;
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

export interface SendChatMessageResult {
  messageId: string;
  sentAtUtc: string;
}

export interface CopyDuetAsGroupResult {
  conversationId: string;
  name: string;
  participants: ConversationParticipant[];
}

export interface ConversationMessagesResult {
  messages: ConversationMessage[];
  nextBeforeSentAtUtc: string | null;
  nextBeforeMessageId: string | null;
  hasMore: boolean;
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

export interface GroupConversation {
  conversationId: string;
  name: string;
  participantCount: number;
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

function mapGroupMessage(dto: ConversationMessageDto): GroupConversationMessage {
  return {
    id: dto.id ?? dto.Id ?? crypto.randomUUID(),
    conversationId: dto.conversationId ?? dto.ConversationId ?? "",
    senderUserId: dto.senderUserId ?? dto.SenderUserId ?? "",
    senderDisplayName: dto.senderDisplayName ?? dto.SenderDisplayName ?? "",
    text: dto.text ?? dto.Text ?? "",
    sentAtUtc: dto.sentAtUtc ?? dto.SentAtUtc ?? new Date().toISOString(),
  };
}

function mapGroupConversation(dto: GroupConversationSummaryDto): GroupConversation {
  return {
    conversationId: dto.conversationId ?? dto.ConversationId ?? "",
    name: dto.name ?? dto.Name ?? "",
    participantCount: dto.participantCount ?? dto.ParticipantCount ?? 0,
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
    messages: (response.items ?? response.Items ?? []).map(mapGroupMessage),
    nextBeforeSentAtUtc: response.nextBeforeSentAtUtc ?? response.NextBeforeSentAtUtc ?? null,
    nextBeforeMessageId: response.nextBeforeMessageId ?? response.NextBeforeMessageId ?? null,
    hasMore: response.hasMore ?? response.HasMore ?? false,
  };
}

export async function sendGroupChatMessage(
  payload: SendChatMessagePayload,
  accessToken: string,
  signal?: AbortSignal,
): Promise<SendGroupChatMessageResult> {
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

export async function fetchGroupConversations(
  accessToken: string,
  signal?: AbortSignal,
): Promise<GroupConversation[]> {
  const response = await getJson<GetGroupConversationsResponseDto>(
    "/api/conversations/group",
    { accessToken, signal },
  );

  const items = response.groupConversations ?? response.GroupConversations ?? [];
  return items.map(mapGroupConversation);
}

export async function createGroupConversation(
  participantUserIds: string[],
  name: string,
  accessToken: string,
): Promise<GroupConversation> {
  const conversationId = crypto.randomUUID();

  const response = await postJson<CreateGroupConversationResponseDto, CreateGroupConversationRequestDto>(
    "/api/conversations/group",
    { conversationId, participantUserIds, name },
    { accessToken },
  );

  return {
    conversationId: response.conversationId ?? response.ConversationId ?? conversationId,
    name: response.name ?? response.Name ?? name,
    participantCount: participantUserIds.length,
  };
}
