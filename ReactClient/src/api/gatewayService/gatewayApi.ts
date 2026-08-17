import { getJson, putJson } from "../httpClient";
import {
  mapDuetConversationWithPresenceToContact,
  mapGroupMessage,
  mapGroupParticipant,
  mapDuetMessage,
  mapDuetParticipant,
  resolveDuetConversationsWithPresence,
} from "./mappers";
import type {
  OpenDuetConversationResult,
  OpenGroupConversationResult,
} from "../../types/chat";
import type { Contact } from "../../types/contacts";
import type { OpenDuetConversationRequest } from "./conversation/commands/openDuetConversation/OpenDuetConversationRequest";
import type { OpenGroupConversationRequest } from "./conversation/commands/openGroupConversation/OpenGroupConversationRequest";
import type { GetDuetConversationsWithPresenceResponseDto } from "./conversation/queries/getDuetConversationsWithPresence/GetDuetConversationsWithPresenceResponseDto";
import type { OpenDuetConversationResponseDto } from "./conversation/queries/openDuetConversation/OpenDuetConversationResponseDto";
import type { OpenGroupConversationResponseDto } from "./conversation/queries/openGroupConversation/OpenGroupConversationResponseDto";

export async function fetchContacts(accessToken: string): Promise<Contact[]> {
  const response = await getJson<GetDuetConversationsWithPresenceResponseDto>(
    "/api/aggregate/conversations/duets",
    { accessToken },
  );

  return resolveDuetConversationsWithPresence(response)
    .map(mapDuetConversationWithPresenceToContact);
}

export async function openDuetConversation(
  partnerUserId: string,
  knownConversationId: string | null,
  accessToken: string,
  signal?: AbortSignal,
): Promise<OpenDuetConversationResult> {
  const response = await putJson<OpenDuetConversationResponseDto, OpenDuetConversationRequest>(
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
    conversationId: response.conversationId ?? "",
    participants: (response.participants ?? []).map(mapDuetParticipant),
    messages: (response.messages ?? []).map(mapDuetMessage),
    nextBeforeSequenceNum: response.nextBeforeSequenceNum ?? null,
    currentSequenceNum: response.currentSequenceNum,
    hasMore: response.hasMore,
  };
}

export async function openGroupConversation(
  conversationId: string,
  accessToken: string,
  signal?: AbortSignal,
): Promise<OpenGroupConversationResult> {
  const response = await putJson<OpenGroupConversationResponseDto, OpenGroupConversationRequest>(
    "/api/aggregate/conversations/group/open",
    { conversationId },
    {
      accessToken,
      signal,
    },
  );

  return {
    conversationId: response.conversationId ?? conversationId,
    name: response.name ?? "",
    participants: (response.participants ?? []).map(mapGroupParticipant),
    messages: (response.messages ?? []).map(mapGroupMessage),
    nextBeforeSequenceNum: response.nextBeforeSequenceNum ?? null,
    currentSequenceNum: response.currentSequenceNum,
    hasMore: response.hasMore,
  };
}
