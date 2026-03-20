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
  status: string;
  changedAtUtc: string;
}

export type RealtimeConnectionStatus =
  | "idle"
  | "connecting"
  | "connected"
  | "reconnecting"
  | "disconnected"
  | "error";
