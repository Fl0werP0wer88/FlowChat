import type { Contact } from "../../types/contacts";
import type {
  DuetConversationMessage,
  DuetConversationParticipant,
  GroupConversationMessage,
  GroupConversationParticipant,
} from "../../types/chat";
import type { DuetConversationWithPresenceDto } from "./conversation/queries/getDuetConversationsWithPresence/DuetConversationWithPresenceDto";
import type { GetDuetConversationsWithPresenceResponseDto } from "./conversation/queries/getDuetConversationsWithPresence/GetDuetConversationsWithPresenceResponseDto";
import type { ConversationMessageDto } from "./conversation/queries/getConversationMessages/ConversationMessageDto";
import type { ConversationParticipantDto } from "./conversation/queries/getConversationParticipants/ConversationParticipantDto";
import { calculateUnreadCount } from "../../utils/chatUtils";

export function resolveDuetConversationsWithPresence(
  response: GetDuetConversationsWithPresenceResponseDto,
): DuetConversationWithPresenceDto[] {
  return response.conversations ?? [];
}

export function mapDuetConversationWithPresenceToContact(
  dto: DuetConversationWithPresenceDto,
): Contact {
  const lastReadMsgSeqNum = dto.lastReadMsgSeqNum ?? 0;
  const currentMsgSeqNum = dto.currentMsgSeqNum ?? 0;

  return {
    userId: dto.partnerUserId ?? "",
    displayName: dto.displayName ?? "Nowy kontakt",
    email: dto.email ?? null,
    status: dto.status ?? "Invisible",
    conversationId: dto.conversationId ?? "",
    lastReadMsgSeqNum,
    currentMsgSeqNum,
    unreadCount: dto.unreadCount ?? calculateUnreadCount(currentMsgSeqNum, lastReadMsgSeqNum),
    isBlocked: dto.isBlocked ?? false,
    isBlockedByPartner: dto.isBlockedByPartner ?? false,
    isMuted: dto.isMuted ?? false,
    isHidden: dto.isHidden ?? false,
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
    text: dto.text ?? "",
    sequenceNum: dto.sequenceNum ?? null,
    sentAtUtc: dto.sentAtUtc ?? new Date().toISOString(),
  };
}
