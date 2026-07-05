import { getJson, putJson } from "../httpClient";
import {
  mapContact,
  mapGroupMessage,
  mapGroupParticipant,
  mapMessage,
  mapParticipant,
  resolveContacts,
} from "./mappers";
import type {
  OpenDuetConversationResult,
  OpenGroupConversationResult,
} from "../../types/chat";
import type { Contact } from "../../types/contacts";
import type { OpenDuetConversationRequest } from "./conversation/commands/openDuetConversation/OpenDuetConversationRequest";
import type { OpenGroupConversationRequest } from "./conversation/commands/openGroupConversation/OpenGroupConversationRequest";
import type { GetContactsResponseDto } from "./contact/queries/getContacts/GetContactsResponseDto";
import type { OpenDuetConversationResponseDto } from "./conversation/queries/openDuetConversation/OpenDuetConversationResponseDto";
import type { OpenGroupConversationResponseDto } from "./conversation/queries/openGroupConversation/OpenGroupConversationResponseDto";

export async function fetchContacts(accessToken: string): Promise<Contact[]> {
  const response = await getJson<GetContactsResponseDto>("/api/aggregate/contacts", {
    accessToken,
  });

  return resolveContacts(response).map(mapContact);
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
    participants: (response.participants ?? []).map(mapParticipant),
    messages: (response.messages ?? []).map(mapMessage),
    nextBeforeSentAtUtc: response.nextBeforeSentAtUtc ?? null,
    nextBeforeMessageId: response.nextBeforeMessageId ?? null,
    hasMore: response.hasMore ?? false,
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
    nextBeforeSentAtUtc: response.nextBeforeSentAtUtc ?? null,
    nextBeforeMessageId: response.nextBeforeMessageId ?? null,
    hasMore: response.hasMore ?? false,
  };
}
