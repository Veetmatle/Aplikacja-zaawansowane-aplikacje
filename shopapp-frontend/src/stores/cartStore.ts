import { create } from 'zustand';
import { v4 as uuidv4 } from 'uuid';

interface CartUIState {
  isOpen: boolean;
  sessionId: string | null;
  openCart: () => void;
  closeCart: () => void;
  toggleCart: () => void;
  ensureSession: () => string;
}

function getOrCreateSessionId(): string {
  let sid = localStorage.getItem('shopapp-session-id');
  if (!sid) {
    sid = uuidv4();
    localStorage.setItem('shopapp-session-id', sid);
  }
  return sid;
}

export const useCartStore = create<CartUIState>()((set, get) => ({
  isOpen: false,
  sessionId: localStorage.getItem('shopapp-session-id'),

  openCart: () => set({ isOpen: true }),
  closeCart: () => set({ isOpen: false }),
  toggleCart: () => set((s) => ({ isOpen: !s.isOpen })),

  ensureSession: () => {
    const { sessionId } = get();
    if (sessionId) return sessionId;
    const newSid = getOrCreateSessionId();
    set({ sessionId: newSid });
    return newSid;
  },
}));
