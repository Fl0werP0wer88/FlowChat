import type { Contact } from "../../../../../types/contacts";

export interface ContactDto {
  contactUserId?: string;
  displayName?: string;
  email?: string | null;
  conversationId?: string;
  lastReadMsgSeqNum?: number;
  currentMsgSeqNum?: number;
  unreadCount?: number;
  status?: Contact["status"];
}
