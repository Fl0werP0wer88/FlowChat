import { getJson, putJson } from "./httpClient";
import type { Contact } from "../types/contacts";

interface ContactDto {
  id?: string;
  contactUserId?: string;
  displayName?: string;
  email?: string | null;
  conversationId?: string | null;
  status?: Contact["status"];
}

interface GetContactsResponseDto {
  contacts?: ContactDto[];
}

interface OpenDuetConversationPayload {
  partnerUserId: string;
  knownConversationId: string | null;
}

interface OpenGroupConversationPayload {
  conversationId: string;
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

interface OpenDuetConversationResponseDto {
  conversationId?: string;
  participants?: ConversationParticipantDto[];
  messages?: ConversationMessageDto[];
  nextBeforeSentAtUtc?: string | null;
  nextBeforeMessageId?: string | null;
  hasMore?: boolean;
}

interface OpenGroupConversationResponseDto {
  conversationId?: string;
  name?: string;
  participants?: ConversationParticipantDto[];
  messages?: ConversationMessageDto[];
  nextBeforeSentAtUtc?: string | null;
  nextBeforeMessageId?: string | null;
  hasMore?: boolean;
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
  const response = await putJson<OpenGroupConversationResponseDto, OpenGroupConversationPayload>(
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
