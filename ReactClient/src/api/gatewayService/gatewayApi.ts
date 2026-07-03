import { getJson, putJson } from "../httpClient";
import type { Contact } from "../../types/contacts";
import type { OpenDuetConversationRequest } from "./conversation/commands/openDuetConversation/OpenDuetConversationRequest";
import type { OpenGroupConversationRequest } from "./conversation/commands/openGroupConversation/OpenGroupConversationRequest";
import type { ContactDto } from "./contact/queries/getContacts/ContactDto";
import type { GetContactsResponseDto } from "./contact/queries/getContacts/GetContactsResponseDto";
import type { ConversationMessageDto } from "./conversation/queries/getConversationMessages/ConversationMessageDto";
import type { ConversationMessage } from "./conversation/queries/getConversationMessages/ConversationMessage";
import type { GroupConversationMessage } from "./conversation/queries/getConversationMessages/GroupConversationMessage";
import type { ConversationParticipantDto } from "./conversation/queries/getConversationParticipants/ConversationParticipantDto";
import type { ConversationParticipant } from "./conversation/queries/getConversationParticipants/ConversationParticipant";
import type { GroupConversationParticipant } from "./conversation/queries/getConversationParticipants/GroupConversationParticipant";
import type { OpenDuetConversationResponseDto } from "./conversation/queries/openDuetConversation/OpenDuetConversationResponseDto";
import type { OpenDuetConversationResult } from "./conversation/queries/openDuetConversation/OpenDuetConversationResult";
import type { OpenGroupConversationResponseDto } from "./conversation/queries/openGroupConversation/OpenGroupConversationResponseDto";
import type { OpenGroupConversationResult } from "./conversation/queries/openGroupConversation/OpenGroupConversationResult";

export type { ConversationMessage } from "./conversation/queries/getConversationMessages/ConversationMessage";
export type { GroupConversationMessage } from "./conversation/queries/getConversationMessages/GroupConversationMessage";
export type { ConversationParticipant } from "./conversation/queries/getConversationParticipants/ConversationParticipant";
export type { GroupConversationParticipant } from "./conversation/queries/getConversationParticipants/GroupConversationParticipant";
export type { OpenDuetConversationResult } from "./conversation/queries/openDuetConversation/OpenDuetConversationResult";
export type { OpenGroupConversationResult } from "./conversation/queries/openGroupConversation/OpenGroupConversationResult";

function resolveContacts(response: GetContactsResponseDto): ContactDto[] {
  return response.contacts ?? [];
}

function mapContact(dto: ContactDto): Contact {
  const userId = dto.contactUserId ?? dto.id ?? crypto.randomUUID();

  return {
    id: dto.id ?? crypto.randomUUID(),
    userId,
    displayName: dto.displayName ?? "Nowy kontakt",
    email: dto.email ?? null,
    status: dto.status ?? "Invisible",
    conversationId: dto.conversationId ?? null,
  };
}

function mapParticipant(dto: ConversationParticipantDto): ConversationParticipant {
  return {
    userId: dto.userId ?? "",
    displayName: dto.displayName ?? null,
    avatarUrl: dto.avatarUrl ?? null,
    participantUserId: dto.participantUserId ?? "",
  };
}

function mapGroupParticipant(dto: ConversationParticipantDto): GroupConversationParticipant {
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
