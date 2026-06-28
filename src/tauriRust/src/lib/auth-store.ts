import { create } from 'zustand';
import type { AuthSession } from './types';

export type AuthStatus = 'idle' | 'checking' | 'authenticated' | 'anonymous';

type AuthState = {
  session: AuthSession | null;
  status: AuthStatus;
  setChecking: () => void;
  setSession: (session: AuthSession) => void;
  clearSession: () => void;
};

export const useAuthStore = create<AuthState>((set) => ({
  session: null,
  status: 'idle',
  setChecking: () => set({ status: 'checking' }),
  setSession: (session) => set({ session, status: 'authenticated' }),
  clearSession: () => set({ session: null, status: 'anonymous' }),
}));

export function getAuthStoreState() {
  return useAuthStore.getState();
}
