import { create } from "zustand";
import type { GroupConversation } from "../api/chatApi";
import type { Contact } from "../types/contacts";

export type ActiveConversationMode = "duet" | "group";
export type SidebarTab = "contacts" | "groups";
export type ActiveComposer =
  | { type: "contacts" }
  | { type: "group"; groupName?: string; initialUserIds?: string[] }
  | null;

interface ChatSelectionStore {
  activeConversationMode: ActiveConversationMode;
  activeContactId: string | null;
  activeGroupConversation: GroupConversation | null;
  activeTab: SidebarTab;
  activeComposer: ActiveComposer;

  selectDuetContact: (contact: Contact) => void;
  selectGroupConversation: (conversation: GroupConversation) => void;
  setActiveTab: (tab: SidebarTab) => void;
  openContactsBuilder: () => void;
  openGroupBuilder: (groupName?: string, initialUserIds?: string[]) => void;
  closeComposer: () => void;
}

export const useChatSelectionStore = create<ChatSelectionStore>((set) => ({
  activeConversationMode: "duet",
  activeContactId: null,
  activeGroupConversation: null,
  activeTab: "contacts",
  activeComposer: null,

  selectDuetContact: (contact) =>
    set({ activeConversationMode: "duet", activeContactId: contact.id }),

  selectGroupConversation: (conversation) =>
    set({ activeConversationMode: "group", activeGroupConversation: conversation }),

  setActiveTab: (tab) => set({ activeTab: tab }),

  openContactsBuilder: () => set({ activeComposer: { type: "contacts" } }),

  openGroupBuilder: (groupName, initialUserIds) =>
    set({ activeTab: "groups", activeComposer: { type: "group", groupName, initialUserIds } }),

  closeComposer: () => set({ activeComposer: null }),
}));
