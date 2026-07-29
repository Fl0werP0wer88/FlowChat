import { deleteJson, getJson, postJson, putJson } from "../httpClient";
import type { SendChatMessageRequest } from "./chatMessage/commands/sendChatMessage/SendChatMessageRequest";
import type { CopyDuetAsGroupRequest } from "./conversation/commands/copyDuetAsGroup/CopyDuetAsGroupRequest";
import type { CreateGroupConversationRequest } from "./conversation/commands/createGroupConversation/CreateGroupConversationRequest";
import type { GetConversationMessagesResponseDto } from "./chatMessage/queries/getConversationMessages/GetConversationMessagesResponseDto";
import type { GetGroupConversationMessagesResponseDto } from "./chatMessage/queries/getGroupConversationMessages/GetGroupConversationMessagesResponseDto";
import type { CatchUpConversationMessagesResponseDto } from "./chatMessage/queries/catchUpConversationMessages/CatchUpConversationMessagesResponseDto";
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
  ConversationMessagesCatchUpResult,
  SendChatMessageResult,
  SendGroupChatMessageResult,
} from "../../types/chat";

export async function getDuetConversationMessages(
  conversationId: string,
  beforeSequenceNum: number | null,
  accessToken: string,
  signal?: AbortSignal,
): Promise<DuetConversationMessagesResult> {
  const params = new URLSearchParams({ limit: "10" });

  if (beforeSequenceNum !== null) {
    params.set("beforeSequenceNum", beforeSequenceNum.toString());
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
    nextBeforeSequenceNum: response.nextBeforeSequenceNum ?? null,
    currentSequenceNum: response.currentSequenceNum,
    hasMore: response.hasMore,
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
    sequenceNum: response.sequenceNum,
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
  beforeSequenceNum: number | null,
  accessToken: string,
  signal?: AbortSignal,
): Promise<GroupConversationMessagesResult> {
  const params = new URLSearchParams({ limit: "10" });

  if (beforeSequenceNum !== null) {
    params.set("beforeSequenceNum", beforeSequenceNum.toString());
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
    nextBeforeSequenceNum: response.nextBeforeSequenceNum ?? null,
    currentSequenceNum: response.currentSequenceNum,
    hasMore: response.hasMore,
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
    sequenceNum: response.sequenceNum,
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
  sequenceNum: number,
  accessToken: string,
  signal?: AbortSignal,
): Promise<void> {
  await putJson<null, { sequenceNum: number }>(
    `/api/conversations/${encodeURIComponent(conversationId)}/read-state`,
    { sequenceNum },
    { accessToken, signal },
  );
}

export async function catchUpConversationMessages(
  conversationId: string,
  afterSequenceNum: number,
  throughSequenceNum: number | null,
  accessToken: string,
  signal?: AbortSignal,
): Promise<ConversationMessagesCatchUpResult> {
  const params = new URLSearchParams({
    afterSequenceNum: afterSequenceNum.toString(),
    limit: "100",
  });
  if (throughSequenceNum !== null) {
    params.set("throughSequenceNum", throughSequenceNum.toString());
  }

  const response = await getJson<CatchUpConversationMessagesResponseDto>(
    `/api/chat/conversations/${encodeURIComponent(conversationId)}/messages/catch-up?${params.toString()}`,
    { accessToken, signal },
  );

  return {
    messages: (response.items ?? []).map(mapDuetMessage),
    nextAfterSequenceNum: response.nextAfterSequenceNum ?? null,
    currentSequenceNum: response.currentSequenceNum,
    throughSequenceNum: response.throughSequenceNum,
    hasMore: response.hasMore,
  };
}

function getParticipantSettingPath(conversationId: string, setting: "mute" | "block" | "hide") {
  return `/api/conversations/${encodeURIComponent(conversationId)}/${setting}`;
}

export async function muteConversation(conversationId: string, accessToken: string): Promise<void> {
  await putJson<null, Record<string, never>>(
    getParticipantSettingPath(conversationId, "mute"),
    {},
    { accessToken },
  );
}

export async function unmuteConversation(conversationId: string, accessToken: string): Promise<void> {
  await deleteJson<null>(getParticipantSettingPath(conversationId, "mute"), { accessToken });
}

export async function blockConversation(conversationId: string, accessToken: string): Promise<void> {
  await putJson<null, Record<string, never>>(
    getParticipantSettingPath(conversationId, "block"),
    {},
    { accessToken },
  );
}

export async function unblockConversation(conversationId: string, accessToken: string): Promise<void> {
  await deleteJson<null>(getParticipantSettingPath(conversationId, "block"), { accessToken });
}

export async function hideConversation(conversationId: string, accessToken: string): Promise<void> {
  await putJson<null, Record<string, never>>(
    getParticipantSettingPath(conversationId, "hide"),
    {},
    { accessToken },
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
