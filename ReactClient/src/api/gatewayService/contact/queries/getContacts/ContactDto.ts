import type { Contact } from "../../../../../types/contacts";

export interface ContactDto {
  id?: string;
  contactUserId?: string;
  displayName?: string;
  email?: string | null;
  conversationId?: string | null;
  status?: Contact["status"];
}
