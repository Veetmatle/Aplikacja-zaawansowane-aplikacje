import api from './client';
import type { CartDto, AddToCartRequest, UpdateCartItemRequest } from '@/types/api';

export const cartApi = {
  createSession: () =>
    api.post<{ sessionId: string }>('/cart/session').then((r) => r.data),

  getCart: () =>
    api.get<CartDto>('/cart').then((r) => r.data),

  addItem: (data: AddToCartRequest) =>
    api.post<CartDto>('/cart/items', data).then((r) => r.data),

  updateItem: (cartItemId: string, data: UpdateCartItemRequest) =>
    api.put<CartDto>(`/cart/items/${cartItemId}`, data).then((r) => r.data),

  removeItem: (cartItemId: string) =>
    api.delete(`/cart/items/${cartItemId}`).then((r) => r.data),

  clear: () =>
    api.delete('/cart').then((r) => r.data),

  merge: (sessionId: string) =>
    api.post('/cart/merge', { sessionId }).then((r) => r.data),
};
