import api from './client';
import type { OrderDto, CreateOrderRequest } from '@/types/api';

export const ordersApi = {
  getOrders: () =>
    api.get<OrderDto[]>('/orders').then((r) => r.data),

  getOrder: (id: string) =>
    api.get<OrderDto>(`/orders/${id}`).then((r) => r.data),

  create: (data: CreateOrderRequest) =>
    api.post<OrderDto>('/orders', data).then((r) => r.data),
};
