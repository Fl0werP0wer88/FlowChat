import type { ConversationMessageDto } from "./chatMessage/queries/getConversationMessages/ConversationMessageDto";
import type { ConversationParticipantDto } from "./conversation/queries/getConversationParticipants/ConversationParticipantDto";
import type { GroupConversationSummaryDto } from "./conversation/queries/getGroupConversations/GroupConversationSummaryDto";
import type {
  DuetConversationMessage,
  DuetConversationParticipant,
  GroupConversation,
  GroupConversationMessage,
  GroupConversationParticipant,
} from "../../types/chat";
import { calculateUnreadCount } from "../../utils/chatUtils";

export function mapDuetParticipant(dto: ConversationParticipantDto): DuetConversationParticipant {
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

export function mapGroupConversation(dto: GroupConversationSummaryDto): GroupConversation {
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

export function mapGroupParticipant(dto: ConversationParticipantDto): GroupConversationParticipant {
  return {
    userId: dto.userId ?? "",
    displayName: dto.displayName ?? null,
    avatarUrl: dto.avatarUrl ?? null,
    participantUserId: dto.participantUserId ?? "",
  };
}
