import { create } from 'zustand';

interface CartUIState {
  isOpen: boolean;
  sessionId: string | null;
  openCart: () => void;
  closeCart: () => void;
  toggleCart: () => void;
  initSession: () => Promise<string>;
}

export const useCartStore = create<CartUIState>()((set, get) => ({
  isOpen: false,
  sessionId: localStorage.getItem('shopapp-session-id'),

  openCart: () => set({ isOpen: true }),
  closeCart: () => set({ isOpen: false }),
  toggleCart: () => set((s) => ({ isOpen: !s.isOpen })),

  initSession: async () => {
    const existing = get().sessionId;
    if (existing) return existing;

    const stored = localStorage.getItem('shopapp-session-id');
    if (stored) {
      set({ sessionId: stored });
      return stored;
    }

    // Request session from backend
    const response = await fetch(
      (import.meta.env.VITE_API_URL || '/api') + '/cart/session',
      { method: 'POST' }
    );
    const data = await response.json();
    const sid: string = data.sessionId;
    localStorage.setItem('shopapp-session-id', sid);
    set({ sessionId: sid });
    return sid;
  },
}));
