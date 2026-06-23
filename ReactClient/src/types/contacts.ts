import type { UserStatus } from "./realtime";

export type ContactStatus = UserStatus;

export interface Contact {
  id: string;
  userId: string;
  displayName: string;
  email: string | null;
  status: ContactStatus;
  conversationId: string | null;
}
