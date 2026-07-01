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
  name?: string;
  participants?: ConversationParticipantDto[];
}

interface SendChatMessageResponseDto {
  messageId?: string;
  sentAtUtc?: string;
}

interface ConversationParticipantDto {
  userId?: string;
  displayName?: string | null;
  avatarUrl?: string | null;
  participantUserId?: string;
}

interface ConversationMessageDto {
  id?: string;
  conversationId?: string;
  senderUserId?: string;
  senderDisplayName?: string;
  text?: string;
  sentAtUtc?: string;
}

interface GetConversationMessagesResponseDto {
  items?: ConversationMessageDto[];
  nextBeforeSentAtUtc?: string | null;
  nextBeforeMessageId?: string | null;
  hasMore?: boolean;
}

interface GetGroupConversationMessagesResponseDto {
  items?: ConversationMessageDto[];
  nextBeforeSentAtUtc?: string | null;
  nextBeforeMessageId?: string | null;
  hasMore?: boolean;
}

interface GroupConversationSummaryDto {
  conversationId?: string;
  name?: string;
  participantCount?: number;
}

interface GetGroupConversationsResponseDto {
  groupConversations?: GroupConversationSummaryDto[];
}

interface CreateGroupConversationRequestDto {
  conversationId: string;
  participantUserIds: string[];
  name: string;
}

interface CreateGroupConversationResponseDto {
  conversationId?: string;
  name?: string;
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
    userId: dto.userId ?? "",
    displayName: dto.displayName ?? null,
    avatarUrl: dto.avatarUrl ?? null,
    participantUserId: dto.participantUserId ?? "",
  };
}

function mapMessage(dto: ConversationMessageDto): ConversationMessage {
  return {
    id: dto.id ?? crypto.randomUUID(),
    conversationId: dto.conversationId ?? "",
    senderUserId: dto.senderUserId ?? "",
    senderDisplayName: dto.senderDisplayName ?? "",
    text: dto.text ?? "",
    sentAtUtc: dto.sentAtUtc ?? new Date().toISOString(),
  };
}

function mapGroupMessage(dto: ConversationMessageDto): GroupConversationMessage {
  return {
    id: dto.id ?? crypto.randomUUID(),
    conversationId: dto.conversationId ?? "",
    senderUserId: dto.senderUserId ?? "",
    senderDisplayName: dto.senderDisplayName ?? "",
    text: dto.text ?? "",
    sentAtUtc: dto.sentAtUtc ?? new Date().toISOString(),
  };
}

function mapGroupConversation(dto: GroupConversationSummaryDto): GroupConversation {
  return {
    conversationId: dto.conversationId ?? "",
    name: dto.name ?? "",
    participantCount: dto.participantCount ?? 0,
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
    messages: (response.items ?? []).map(mapMessage),
    nextBeforeSentAtUtc: response.nextBeforeSentAtUtc ?? null,
    nextBeforeMessageId: response.nextBeforeMessageId ?? null,
    hasMore: response.hasMore ?? false,
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
    messageId: response.messageId ?? payload.id,
    sentAtUtc: response.sentAtUtc ?? new Date().toISOString(),
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
    conversationId: response.conversationId ?? newGroupConversationId,
    name: response.name ?? "",
    participants: (response.participants ?? []).map(mapParticipant),
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
    messages: (response.items ?? []).map(mapGroupMessage),
    nextBeforeSentAtUtc: response.nextBeforeSentAtUtc ?? null,
    nextBeforeMessageId: response.nextBeforeMessageId ?? null,
    hasMore: response.hasMore ?? false,
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
    messageId: response.messageId ?? payload.id,
    sentAtUtc: response.sentAtUtc ?? new Date().toISOString(),
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

  const items = response.groupConversations ?? [];
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
    conversationId: response.conversationId ?? conversationId,
    name: response.name ?? name,
    participantCount: participantUserIds.length,
  };
}
