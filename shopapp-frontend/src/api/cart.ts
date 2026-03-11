import api from './client';
import type { CartDto, AddToCartRequest, UpdateCartItemRequest } from '@/types/api';

export const cartApi = {
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
};
