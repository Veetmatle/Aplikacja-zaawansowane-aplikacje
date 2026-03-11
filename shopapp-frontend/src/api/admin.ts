import api from './client';
import type { UserDto, UpdateUserRequest, BanUserRequest, SetTimeoutRequest, AssignRoleRequest, PagedResult } from '@/types/api';

export const adminApi = {
  getUsers: (params: { page?: number; pageSize?: number; search?: string }) =>
    api.get<PagedResult<UserDto>>('/admin/users', { params }).then((r) => r.data),

  getUser: (id: string) =>
    api.get<UserDto>(`/admin/users/${id}`).then((r) => r.data),

  banUser: (id: string, data: BanUserRequest) =>
    api.post(`/admin/users/${id}/ban`, data).then((r) => r.data),

  unbanUser: (id: string) =>
    api.post(`/admin/users/${id}/unban`).then((r) => r.data),

  setTimeout: (id: string, data: SetTimeoutRequest) =>
    api.post(`/admin/users/${id}/timeout`, data).then((r) => r.data),

  removeTimeout: (id: string) =>
    api.delete(`/admin/users/${id}/timeout`).then((r) => r.data),

  assignRole: (id: string, data: AssignRoleRequest) =>
    api.post(`/admin/users/${id}/roles`, data).then((r) => r.data),

  removeRole: (id: string, roleName: string) =>
    api.delete(`/admin/users/${id}/roles/${roleName}`).then((r) => r.data),

  deleteUser: (id: string) =>
    api.delete(`/admin/users/${id}`).then((r) => r.data),
};

export const usersApi = {
  getMe: () =>
    api.get<UserDto>('/users/me').then((r) => r.data),

  updateMe: (data: UpdateUserRequest) =>
    api.put<UserDto>('/users/me', data).then((r) => r.data),
};
