import { create } from 'zustand';

let initPromise: Promise<string> | null = null;

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
    // Return existing session
    const existing = get().sessionId || localStorage.getItem('shopapp-session-id');
    if (existing) {
      set({ sessionId: existing });
      return existing;
    }

    // Deduplicate concurrent calls
    if (initPromise) return initPromise;

    initPromise = (async () => {
      const response = await fetch(
        (import.meta.env.VITE_API_URL || '/api') + '/cart/session',
        { method: 'POST' }
      );
      const data = await response.json();
      const sid: string = data.sessionId;
      localStorage.setItem('shopapp-session-id', sid);
      set({ sessionId: sid });
      return sid;
    })();

    try {
      return await initPromise;
    } finally {
      initPromise = null;
    }
  },
}));
