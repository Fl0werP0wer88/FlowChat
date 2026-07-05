import { getJson, postJson, putJson } from "../httpClient";
import type { SendChatMessageRequest } from "./chatMessage/commands/sendChatMessage/SendChatMessageRequest";
import type { CopyDuetAsGroupRequest } from "./conversation/commands/copyDuetAsGroup/CopyDuetAsGroupRequest";
import type { CreateGroupConversationRequest } from "./conversation/commands/createGroupConversation/CreateGroupConversationRequest";
import type { GetConversationMessagesResponseDto } from "./chatMessage/queries/getConversationMessages/GetConversationMessagesResponseDto";
import type { GetGroupConversationMessagesResponseDto } from "./chatMessage/queries/getGroupConversationMessages/GetGroupConversationMessagesResponseDto";
import type { SendChatMessageResponseDto } from "./chatMessage/queries/sendChatMessage/SendChatMessageResponseDto";
import type { CopyDuetAsGroupResponseDto } from "./conversation/queries/copyDuetAsGroup/CopyDuetAsGroupResponseDto";
import type { GetGroupConversationsResponseDto } from "./conversation/queries/getGroupConversations/GetGroupConversationsResponseDto";
import type { CreateGroupConversationResponseDto } from "./conversation/queries/createGroupConversation/CreateGroupConversationResponseDto";
import {
  mapGroupConversation,
  mapGroupMessage,
  mapDuetMessage,
  mapDuetParticipant,
} from "./mappers";
import type {
  DuetConversationMessagesResult,
  CopyDuetAsGroupResult,
  GroupConversation,
  GroupConversationMessagesResult,
  SendChatMessageResult,
  SendGroupChatMessageResult,
} from "../../types/chat";

export async function getDuetConversationMessages(
  conversationId: string,
  cursor: { beforeSentAtUtc: string | null; beforeMessageId: string | null },
  accessToken: string,
  signal?: AbortSignal,
): Promise<DuetConversationMessagesResult> {
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
    messages: (response.items ?? []).map(mapDuetMessage),
    nextBeforeSentAtUtc: response.nextBeforeSentAtUtc ?? null,
    nextBeforeMessageId: response.nextBeforeMessageId ?? null,
    hasMore: response.hasMore ?? false,
  };
}

export async function sendChatMessage(
  payload: SendChatMessageRequest,
  accessToken: string,
  signal?: AbortSignal,
): Promise<SendChatMessageResult> {
  const response = await putJson<SendChatMessageResponseDto, SendChatMessageRequest>(
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
  const response = await postJson<CopyDuetAsGroupResponseDto, CopyDuetAsGroupRequest>(
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
    participants: (response.participants ?? []).map(mapDuetParticipant),
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
  payload: SendChatMessageRequest,
  accessToken: string,
  signal?: AbortSignal,
): Promise<SendGroupChatMessageResult> {
  const response = await putJson<SendChatMessageResponseDto, SendChatMessageRequest>(
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

export async function markConversationAsRead(
  conversationId: string,
  accessToken: string,
  signal?: AbortSignal,
): Promise<void> {
  await putJson<null, Record<string, never>>(
    `/api/conversations/${encodeURIComponent(conversationId)}/read-state`,
    {},
    { accessToken, signal },
  );
}

export async function createGroupConversation(
  participantUserIds: string[],
  name: string,
  accessToken: string,
): Promise<GroupConversation> {
  const conversationId = crypto.randomUUID();

  const response = await postJson<CreateGroupConversationResponseDto, CreateGroupConversationRequest>(
    "/api/conversations/group",
    { conversationId, participantUserIds, name },
    { accessToken },
  );

  return {
    conversationId: response.conversationId ?? conversationId,
    name: response.name ?? name,
    participantCount: participantUserIds.length,
    lastReadMsgSeqNum: 0,
    currentMsgSeqNum: 0,
    unreadCount: 0,
  };
}
