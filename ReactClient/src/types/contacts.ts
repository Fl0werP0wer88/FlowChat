export type ContactStatus = "online" | "away" | "offline";

export interface Contact {
  id: number;
  displayName: string;
  status: ContactStatus;
}
