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

  it('isAdmin returns false when user is null', () => {
    expect(useAuthStore.getState().isAdmin()).toBe(false);
  });

  it('isAdmin returns false when roles is undefined (corrupted state)', () => {
    // Simulate corrupted localStorage rehydration
    useAuthStore.setState({
      user: { userId: 'x', email: 'x', firstName: 'x', lastName: 'x', roles: undefined as unknown as string[] },
      isAuthenticated: true,
      accessToken: 'tok',
      refreshToken: 'ref',
    });
    expect(useAuthStore.getState().isAdmin()).toBe(false);
  });

  it('setAuth handles missing roles in response', () => {
    const noRolesResponse = { ...mockAuthResponse, roles: undefined as unknown as string[] };
    useAuthStore.getState().setAuth(noRolesResponse);
    const state = useAuthStore.getState();
    expect(Array.isArray(state.user?.roles)).toBe(true);
    expect(state.user?.roles).toEqual([]);
  });
});
