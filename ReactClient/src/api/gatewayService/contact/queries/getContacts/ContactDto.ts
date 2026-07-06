import type { Contact } from "../../../../../types/contacts";

export interface ContactDto {
  id?: string;
  contactUserId?: string;
  displayName?: string;
  email?: string | null;
  conversationId?: string | null;
  lastReadMsgSeqNum?: number;
  currentMsgSeqNum?: number;
  unreadCount?: number;
  status?: Contact["status"];
}
