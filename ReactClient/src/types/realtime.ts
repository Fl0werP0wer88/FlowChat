export type UserStatus = "Active" | "AFK" | "Busy" | "Invisible";
export type ManualUserStatus = Exclude<UserStatus, "AFK">;

export interface ChatMessageReceivedEvent {
  messageId: string;
  conversationId: string;
  senderUserId: string;
  text: string;
  sequenceNum: number;
  sentAtUtc: string;
}

export interface PresenceChangedEvent {
  userId: string;
  status: UserStatus;
  changedAtUtc: string;
}

export interface GroupConversationChangedEvent {
  conversationId: string;
  type: number;
  name: string | null;
  createdByUserId: string;
}

export interface ConversationParticipantsAddedEvent {
  conversationId: string;
  conversationType: number;
  participantUserIds: string[];
}

export interface ConversationParticipantsRemovedEvent {
  conversationId: string;
  conversationType: number;
  participantUserIds: string[];
}

export type RealtimeConnectionStatus =
  | "idle"
  | "connecting"
  | "connected"
  | "reconnecting"
  | "disconnected"
  | "error";
