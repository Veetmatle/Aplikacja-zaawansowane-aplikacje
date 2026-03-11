import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { cartApi } from '@/api/cart';
import { useCartStore } from '@/stores/cartStore';
import { toast } from 'sonner';
import type { AddToCartRequest, UpdateCartItemRequest } from '@/types/api';

export function useCart() {
  return useQuery({
    queryKey: ['cart'],
    queryFn: () => cartApi.getCart(),
    staleTime: 30 * 1000, // 30s — cart changes often
    select: (data) => ({
      ...data,
      // Compute totalItems in case backend doesn't provide it
      totalItems: data.totalItems ?? data.items.reduce((sum, it) => sum + it.quantity, 0),
    }),
  });
}

export function useAddToCart() {
  const qc = useQueryClient();
  const openCart = useCartStore((s) => s.openCart);

  return useMutation({
    mutationFn: (data: AddToCartRequest) => cartApi.addItem(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['cart'] });
      toast.success('Dodano do koszyka!');
      openCart();
    },
  });
}

export function useUpdateCartItem() {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: ({ cartItemId, data }: { cartItemId: string; data: UpdateCartItemRequest }) =>
      cartApi.updateItem(cartItemId, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['cart'] });
    },
  });
}

export function useRemoveCartItem() {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: (cartItemId: string) => cartApi.removeItem(cartItemId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['cart'] });
      toast.success('Usunięto z koszyka.');
    },
  });
}

export function useClearCart() {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: () => cartApi.clear(),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['cart'] });
    },
  });
}
