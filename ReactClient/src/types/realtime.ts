export type UserStatus = "Active" | "AFK" | "Busy" | "Invisible";
export type ManualUserStatus = Exclude<UserStatus, "AFK">;

export interface RealtimeChatMessage {
  messageId: string;
  conversationId: string;
  senderUserId: string;
  senderDisplayName: string;
  text: string;
  sentAtUtc: string;
}

export interface PresenceChangedEvent {
  userId: string;
  status: UserStatus;
  changedAtUtc: string;
}

export type ContactPresenceStatusesEvent = PresenceChangedEvent[];

export interface PresencePreferencesEvent {
  preferredStatus: UserStatus | null;
}

export type RealtimeConnectionStatus =
  | "idle"
  | "connecting"
  | "connected"
  | "reconnecting"
  | "disconnected"
  | "error";
