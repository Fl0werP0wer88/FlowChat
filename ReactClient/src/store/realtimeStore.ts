import { create } from "zustand";
import type { RealtimeConnectionStatus } from "../types/realtime";

interface RealtimeStore {
  status: RealtimeConnectionStatus;
  lastError: string | null;

  setStatus: (status: RealtimeConnectionStatus) => void;
  setLastError: (error: string | null) => void;
}

export const useRealtimeStore = create<RealtimeStore>((set) => ({
  status: "idle",
  lastError: null,

  setStatus: (status) => set({ status }),
  setLastError: (error) => set({ lastError: error }),
}));
