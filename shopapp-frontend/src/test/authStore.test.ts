import { describe, it, expect, beforeEach } from 'vitest';
import { useAuthStore } from '@/stores/authStore';
import type { AuthResponse } from '@/types/api';

const mockAuthResponse: AuthResponse = {
  accessToken: 'test-access-token',
  refreshToken: 'test-refresh-token',
  expiresAt: '2024-12-31T23:59:59Z',
  userId: 'user-123',
  email: 'test@example.com',
  firstName: 'Jan',
  lastName: 'Kowalski',
  roles: ['User'],
};

describe('authStore', () => {
  beforeEach(() => {
    useAuthStore.getState().logout();
  });

  it('starts unauthenticated', () => {
    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(false);
    expect(state.user).toBeNull();
    expect(state.accessToken).toBeNull();
  });

  it('setAuth stores user and tokens', () => {
    useAuthStore.getState().setAuth(mockAuthResponse);
    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(true);
    expect(state.user?.email).toBe('test@example.com');
    expect(state.user?.firstName).toBe('Jan');
    expect(state.accessToken).toBe('test-access-token');
    expect(state.refreshToken).toBe('test-refresh-token');
  });

  it('logout clears state', () => {
    useAuthStore.getState().setAuth(mockAuthResponse);
    useAuthStore.getState().logout();
    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(false);
    expect(state.user).toBeNull();
    expect(state.accessToken).toBeNull();
  });

  it('setTokens updates tokens', () => {
    useAuthStore.getState().setAuth(mockAuthResponse);
    useAuthStore.getState().setTokens('new-access', 'new-refresh');
    const state = useAuthStore.getState();
    expect(state.accessToken).toBe('new-access');
    expect(state.refreshToken).toBe('new-refresh');
    expect(state.isAuthenticated).toBe(true);
  });

  it('isAdmin returns true when user has Admin role', () => {
    const adminResponse = { ...mockAuthResponse, roles: ['User', 'Admin'] };
    useAuthStore.getState().setAuth(adminResponse);
    expect(useAuthStore.getState().isAdmin()).toBe(true);
  });

  it('isAdmin returns false for regular user', () => {
    useAuthStore.getState().setAuth(mockAuthResponse);
    expect(useAuthStore.getState().isAdmin()).toBe(false);
  });
});
