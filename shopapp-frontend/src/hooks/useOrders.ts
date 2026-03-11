import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ordersApi } from '@/api/orders';
import { toast } from 'sonner';
import type { CreateOrderRequest } from '@/types/api';

export function useOrders() {
  return useQuery({
    queryKey: ['orders'],
    queryFn: () => ordersApi.getOrders(),
  });
}

export function useOrder(id: string) {
  return useQuery({
    queryKey: ['orders', id],
    queryFn: () => ordersApi.getOrder(id),
    enabled: !!id,
  });
}

export function useCreateOrder() {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateOrderRequest) => ordersApi.create(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['orders'] });
      qc.invalidateQueries({ queryKey: ['cart'] });
      toast.success('Zamówienie złożone!');
    },
    onError: (error: Error) => {
      if (error.message.includes('concurrent') || error.message.includes('409')) {
        toast.error('Produkt właśnie się wyprzedał lub zmienił cenę. Odświeżamy koszyk...');
        qc.invalidateQueries({ queryKey: ['cart'] });
        qc.invalidateQueries({ queryKey: ['items'] });
      }
    },
  });
}
