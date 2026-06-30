import { create } from "zustand";
import type { UserStatus } from "../types/realtime";

interface PresenceStore {
  currentStatus: UserStatus;
  preferredStatus: UserStatus | null;
  isUpdatingStatus: boolean;
  errorMessage: string | null;

  applyPresenceState: (currentStatus: UserStatus, preferredStatus: UserStatus | null) => void;
  setIsUpdatingStatus: (updating: boolean) => void;
  setErrorMessage: (message: string | null) => void;
}

export const usePresenceStore = create<PresenceStore>((set) => ({
  currentStatus: "Active",
  preferredStatus: null,
  isUpdatingStatus: false,
  errorMessage: null,

  applyPresenceState: (currentStatus, preferredStatus) => set({ currentStatus, preferredStatus }),
  setIsUpdatingStatus: (updating) => set({ isUpdatingStatus: updating }),
  setErrorMessage: (message) => set({ errorMessage: message }),
}));
