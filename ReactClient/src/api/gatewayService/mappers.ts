import type { Contact } from "../../types/contacts";
import type {
  DuetConversationMessage,
  DuetConversationParticipant,
  GroupConversationMessage,
  GroupConversationParticipant,
} from "../../types/chat";
import type { ContactDto } from "./contact/queries/getContacts/ContactDto";
import type { GetContactsResponseDto } from "./contact/queries/getContacts/GetContactsResponseDto";
import type { ConversationMessageDto } from "./conversation/queries/getConversationMessages/ConversationMessageDto";
import type { ConversationParticipantDto } from "./conversation/queries/getConversationParticipants/ConversationParticipantDto";
import { calculateUnreadCount } from "../../utils/chatUtils";

export function resolveContacts(response: GetContactsResponseDto): ContactDto[] {
  return response.contacts ?? [];
}

export function mapContact(dto: ContactDto): Contact {
  const lastReadMsgSeqNum = dto.lastReadMsgSeqNum ?? 0;
  const currentMsgSeqNum = dto.currentMsgSeqNum ?? 0;

  return {
    userId: dto.contactUserId ?? "",
    displayName: dto.displayName ?? "Nowy kontakt",
    email: dto.email ?? null,
    status: dto.status ?? "Invisible",
    conversationId: dto.conversationId ?? "",
    lastReadMsgSeqNum,
    currentMsgSeqNum,
    unreadCount: dto.unreadCount ?? calculateUnreadCount(currentMsgSeqNum, lastReadMsgSeqNum),
  };
}

export function mapDuetParticipant(dto: ConversationParticipantDto): DuetConversationParticipant {
  return {
    userId: dto.userId ?? "",
    displayName: dto.displayName ?? null,
    avatarUrl: dto.avatarUrl ?? null,
    participantUserId: dto.participantUserId ?? "",
  };
}

export function mapGroupParticipant(dto: ConversationParticipantDto): GroupConversationParticipant {
  return {
    userId: dto.userId ?? "",
    displayName: dto.displayName ?? null,
    avatarUrl: dto.avatarUrl ?? null,
    participantUserId: dto.participantUserId ?? "",
  };
}

export function mapDuetMessage(dto: ConversationMessageDto): DuetConversationMessage {
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

export function mapGroupMessage(dto: ConversationMessageDto): GroupConversationMessage {
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
