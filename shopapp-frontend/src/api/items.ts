import api from './client';
import type { ItemDto, ItemSummaryDto, CreateItemRequest, UpdateItemRequest, ItemQueryParams, PagedResult } from '@/types/api';

export const itemsApi = {
  getItems: (params: ItemQueryParams) =>
    api.get<PagedResult<ItemSummaryDto>>('/items', { params }).then((r) => r.data),

  getItem: (id: string) =>
    api.get<ItemDto>(`/items/${id}`).then((r) => r.data),

  getMyItems: () =>
    api.get<ItemSummaryDto[]>('/items/my').then((r) => r.data),

  create: (data: CreateItemRequest) =>
    api.post<ItemDto>('/items', data).then((r) => r.data),

  update: (id: string, data: UpdateItemRequest) =>
    api.put<ItemDto>(`/items/${id}`, data).then((r) => r.data),

  delete: (id: string) =>
    api.delete(`/items/${id}`).then((r) => r.data),
};
