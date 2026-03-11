import { useQuery, useMutation } from '@tanstack/react-query';
import { paymentsApi } from '@/api/payments';

export function useInitiatePayment() {
  return useMutation({
    mutationFn: (orderId: string) => paymentsApi.initiate(orderId),
    onSuccess: (data) => {
      if (data.redirectUrl) {
        window.location.href = data.redirectUrl;
      }
    },
  });
}

export function usePaymentStatus(orderId: string, enabled = true) {
  return useQuery({
    queryKey: ['payments', orderId],
    queryFn: () => paymentsApi.getStatus(orderId),
    enabled: !!orderId && enabled,
    refetchInterval: 3000, // poll every 3s
  });
}
