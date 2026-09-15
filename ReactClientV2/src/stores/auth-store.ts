import { create } from 'zustand';

import type { AuthSession } from '@/types/auth';

export type AuthBootstrapStatus = 'idle' | 'loading' | 'ready';

interface AuthStore {
  session: AuthSession | null;
  bootstrapStatus: AuthBootstrapStatus;
  setSession: (session: AuthSession) => void;
  clearSession: () => void;
  setBootstrapStatus: (status: AuthBootstrapStatus) => void;
}

export const useAuthStore = create<AuthStore>((set) => ({
  session: null,
  bootstrapStatus: 'idle',
  setSession: (session) => set({ session }),
  clearSession: () => set({ session: null }),
  setBootstrapStatus: (bootstrapStatus) => set({ bootstrapStatus }),
}));

export const getAuthState = () => useAuthStore.getState();
