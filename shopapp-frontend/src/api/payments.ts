import api from './client';
import type { PaymentStatusDto } from '@/types/api';

export const paymentsApi = {
  initiate: (orderId: string) =>
    api.post<PaymentStatusDto>(`/payments/${orderId}/initiate`).then((r) => r.data),

  getStatus: (orderId: string) =>
    api.get<PaymentStatusDto>(`/payments/${orderId}/status`).then((r) => r.data),
};
