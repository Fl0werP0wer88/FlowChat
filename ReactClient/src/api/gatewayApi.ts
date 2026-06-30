import { getJson, putJson } from "./httpClient";
import type { Contact } from "../types/contacts";

interface ContactDto {
  id?: string;
  Id?: string;
  contactUserId?: string;
  ContactUserId?: string;
  displayName?: string;
  DisplayName?: string;
  email?: string | null;
  Email?: string | null;
  conversationId?: string | null;
  ConversationId?: string | null;
  status?: Contact["status"];
  Status?: Contact["status"];
}

interface GetContactsResponseDto {
  contacts?: ContactDto[];
  Contacts?: ContactDto[];
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

interface OpenGroupConversationResponseDto {
  conversationId?: string;
  ConversationId?: string;
  name?: string;
  Name?: string;
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
  return response.contacts ?? response.Contacts ?? [];
}

function mapContact(dto: ContactDto): Contact {
  const userId = dto.contactUserId ?? dto.ContactUserId ?? dto.id ?? dto.Id ?? crypto.randomUUID();

  return {
    id: dto.id ?? dto.Id ?? crypto.randomUUID(),
    userId,
    displayName: dto.displayName ?? dto.DisplayName ?? "Nowy kontakt",
    email: dto.email ?? dto.Email ?? null,
    status: dto.status ?? dto.Status ?? "Invisible",
    conversationId: dto.conversationId ?? dto.ConversationId ?? null,
  };
}

function mapParticipant(dto: ConversationParticipantDto): ConversationParticipant {
  return {
    userId: dto.userId ?? dto.UserId ?? "",
    displayName: dto.displayName ?? dto.DisplayName ?? null,
    avatarUrl: dto.avatarUrl ?? dto.AvatarUrl ?? null,
    participantUserId: dto.participantUserId ?? dto.ParticipantUserId ?? "",
  };
}

function mapGroupParticipant(dto: ConversationParticipantDto): GroupConversationParticipant {
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
    conversationId: response.conversationId ?? response.ConversationId ?? "",
    participants: (response.participants ?? response.Participants ?? []).map(mapParticipant),
    messages: (response.messages ?? response.Messages ?? []).map(mapMessage),
    nextBeforeSentAtUtc: response.nextBeforeSentAtUtc ?? response.NextBeforeSentAtUtc ?? null,
    nextBeforeMessageId: response.nextBeforeMessageId ?? response.NextBeforeMessageId ?? null,
    hasMore: response.hasMore ?? response.HasMore ?? false,
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
    conversationId: response.conversationId ?? response.ConversationId ?? conversationId,
    name: response.name ?? response.Name ?? "",
    participants: (response.participants ?? response.Participants ?? []).map(mapGroupParticipant),
    messages: (response.messages ?? response.Messages ?? []).map(mapGroupMessage),
    nextBeforeSentAtUtc: response.nextBeforeSentAtUtc ?? response.NextBeforeSentAtUtc ?? null,
    nextBeforeMessageId: response.nextBeforeMessageId ?? response.NextBeforeMessageId ?? null,
    hasMore: response.hasMore ?? response.HasMore ?? false,
  };
}
