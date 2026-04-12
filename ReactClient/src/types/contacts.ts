import type { UserStatus } from "./realtime";

export type ContactStatus = UserStatus;

export interface Contact {
  id: string;
  displayName: string;
  email: string | null;
  status: ContactStatus;
}
