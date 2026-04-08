export type ContactStatus = "online" | "away" | "offline";

export interface Contact {
  id: string;
  displayName: string;
  status: ContactStatus;
}
