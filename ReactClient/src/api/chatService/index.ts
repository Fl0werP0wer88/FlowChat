import { getJson, postJson, putJson } from "../httpClient";
import type { SendChatMessageRequest } from "./chatMessage/commands/sendChatMessage/SendChatMessageRequest";
import type { CopyDuetAsGroupRequest } from "./conversation/commands/copyDuetAsGroup/CopyDuetAsGroupRequest";
import type { CreateGroupConversationRequest } from "./conversation/commands/createGroupConversation/CreateGroupConversationRequest";
import type { ConversationMessageDto } from "./chatMessage/queries/getConversationMessages/ConversationMessageDto";
import type { ConversationMessage } from "./chatMessage/queries/getConversationMessages/ConversationMessage";
import type { GetConversationMessagesResponseDto } from "./chatMessage/queries/getConversationMessages/GetConversationMessagesResponseDto";
import type { ConversationMessagesResult } from "./chatMessage/queries/getConversationMessages/ConversationMessagesResult";
import type { GetGroupConversationMessagesResponseDto } from "./chatMessage/queries/getGroupConversationMessages/GetGroupConversationMessagesResponseDto";
import type { GroupConversationMessage } from "./chatMessage/queries/getGroupConversationMessages/GroupConversationMessage";
import type { GroupConversationMessagesResult } from "./chatMessage/queries/getGroupConversationMessages/GroupConversationMessagesResult";
import type { SendChatMessageResponseDto } from "./chatMessage/queries/sendChatMessage/SendChatMessageResponseDto";
import type { SendChatMessageResult } from "./chatMessage/queries/sendChatMessage/SendChatMessageResult";
import type { SendGroupChatMessageResult } from "./chatMessage/queries/sendChatMessage/SendGroupChatMessageResult";
import type { ConversationParticipantDto } from "./conversation/queries/getConversationParticipants/ConversationParticipantDto";
import type { ConversationParticipant } from "./conversation/queries/getConversationParticipants/ConversationParticipant";
import type { GroupConversationParticipant } from "./conversation/queries/getConversationParticipants/GroupConversationParticipant";
import type { CopyDuetAsGroupResponseDto } from "./conversation/queries/copyDuetAsGroup/CopyDuetAsGroupResponseDto";
import type { CopyDuetAsGroupResult } from "./conversation/queries/copyDuetAsGroup/CopyDuetAsGroupResult";
import type { GroupConversationSummaryDto } from "./conversation/queries/getGroupConversations/GroupConversationSummaryDto";
import type { GetGroupConversationsResponseDto } from "./conversation/queries/getGroupConversations/GetGroupConversationsResponseDto";
import type { GroupConversation } from "./conversation/queries/getGroupConversations/GroupConversation";
import type { CreateGroupConversationResponseDto } from "./conversation/queries/createGroupConversation/CreateGroupConversationResponseDto";

export type { ConversationMessage } from "./chatMessage/queries/getConversationMessages/ConversationMessage";
export type { ConversationMessagesResult } from "./chatMessage/queries/getConversationMessages/ConversationMessagesResult";
export type { GroupConversationMessage } from "./chatMessage/queries/getGroupConversationMessages/GroupConversationMessage";
export type { GroupConversationMessagesResult } from "./chatMessage/queries/getGroupConversationMessages/GroupConversationMessagesResult";
export type { SendChatMessageResult } from "./chatMessage/queries/sendChatMessage/SendChatMessageResult";
export type { SendGroupChatMessageResult } from "./chatMessage/queries/sendChatMessage/SendGroupChatMessageResult";
export type { ConversationParticipant } from "./conversation/queries/getConversationParticipants/ConversationParticipant";
export type { GroupConversationParticipant } from "./conversation/queries/getConversationParticipants/GroupConversationParticipant";
export type { CopyDuetAsGroupResult } from "./conversation/queries/copyDuetAsGroup/CopyDuetAsGroupResult";
export type { GroupConversation } from "./conversation/queries/getGroupConversations/GroupConversation";

export function calculateUnreadCount(currentMsgSeqNum: number, lastReadMsgSeqNum: number): number {
  return Math.max(0, currentMsgSeqNum - lastReadMsgSeqNum);
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
    sequenceNum: dto.sequenceNum ?? null,
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
    sequenceNum: dto.sequenceNum ?? null,
    sentAtUtc: dto.sentAtUtc ?? new Date().toISOString(),
  };
}

function mapGroupConversation(dto: GroupConversationSummaryDto): GroupConversation {
  const lastReadMsgSeqNum = dto.lastReadMsgSeqNum ?? 0;
  const currentMsgSeqNum = dto.currentMsgSeqNum ?? 0;

  return {
    conversationId: dto.conversationId ?? "",
    name: dto.name ?? "",
    participantCount: dto.participantCount ?? 0,
    lastReadMsgSeqNum,
    currentMsgSeqNum,
    unreadCount: calculateUnreadCount(currentMsgSeqNum, lastReadMsgSeqNum),
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
