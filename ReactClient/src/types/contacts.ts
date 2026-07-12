import type { UserStatus } from "./realtime";

export type ContactStatus = UserStatus;

export interface Contact {
  userId: string;
  displayName: string;
  email: string | null;
  status: ContactStatus;
  conversationId: string;
  lastReadMsgSeqNum: number;
  currentMsgSeqNum: number;
  unreadCount: number;
  isBlocked: boolean;
  isBlockedByPartner: boolean;
  isMuted: boolean;
  isHidden: boolean;
}
