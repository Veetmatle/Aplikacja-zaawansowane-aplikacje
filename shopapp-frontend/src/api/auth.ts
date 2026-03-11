import api from './client';
import type { AuthResponse, LoginRequest, RegisterRequest, RefreshTokenRequest, ChangePasswordRequest } from '@/types/api';

export const authApi = {
  login: (data: LoginRequest) =>
    api.post<AuthResponse>('/auth/login', data).then((r) => r.data),

  register: (data: RegisterRequest) =>
    api.post<AuthResponse>('/auth/register', data).then((r) => r.data),

  refresh: (data: RefreshTokenRequest) =>
    api.post<AuthResponse>('/auth/refresh', data).then((r) => r.data),

  changePassword: (data: ChangePasswordRequest) =>
    api.post('/auth/change-password', data).then((r) => r.data),

  logout: () =>
    api.post('/auth/logout').then((r) => r.data),
};
