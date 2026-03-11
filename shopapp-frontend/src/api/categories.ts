import api from './client';
import type { CategoryDto, CreateCategoryRequest, UpdateCategoryRequest } from '@/types/api';

export const categoriesApi = {
  getAll: () =>
    api.get<CategoryDto[]>('/categories').then((r) => r.data),

  getById: (id: string) =>
    api.get<CategoryDto>(`/categories/${id}`).then((r) => r.data),

  create: (data: CreateCategoryRequest) =>
    api.post<CategoryDto>('/categories', data).then((r) => r.data),

  update: (id: string, data: UpdateCategoryRequest) =>
    api.put<CategoryDto>(`/categories/${id}`, data).then((r) => r.data),

  delete: (id: string) =>
    api.delete(`/categories/${id}`).then((r) => r.data),
};
