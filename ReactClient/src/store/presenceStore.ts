import { create } from "zustand";
import type { UserStatus } from "../types/realtime";

interface PresenceStore {
  currentStatus: UserStatus;
  preferredStatus: UserStatus | null;
  isUpdatingStatus: boolean;

  applyPresenceState: (currentStatus: UserStatus, preferredStatus: UserStatus | null) => void;
  setIsUpdatingStatus: (updating: boolean) => void;
}

export const usePresenceStore = create<PresenceStore>((set) => ({
  currentStatus: "Active",
  preferredStatus: null,
  isUpdatingStatus: false,

  applyPresenceState: (currentStatus, preferredStatus) => set({ currentStatus, preferredStatus }),
  setIsUpdatingStatus: (updating) => set({ isUpdatingStatus: updating }),
}));
