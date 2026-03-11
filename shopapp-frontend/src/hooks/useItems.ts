import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { itemsApi } from '@/api/items';
import { toast } from 'sonner';
import { useNavigate } from 'react-router-dom';
import type { ItemQueryParams, CreateItemRequest, UpdateItemRequest } from '@/types/api';

export function useItems(params: ItemQueryParams) {
  return useQuery({
    queryKey: ['items', params],
    queryFn: () => itemsApi.getItems(params),
    placeholderData: (prev) => prev,
  });
}

export function useItem(id: string) {
  return useQuery({
    queryKey: ['items', id],
    queryFn: () => itemsApi.getItem(id),
    enabled: !!id,
  });
}

export function useMyItems() {
  return useQuery({
    queryKey: ['items', 'my'],
    queryFn: () => itemsApi.getMyItems(),
  });
}

export function useCreateItem() {
  const qc = useQueryClient();
  const navigate = useNavigate();

  return useMutation({
    mutationFn: (data: CreateItemRequest) => itemsApi.create(data),
    onSuccess: (item) => {
      qc.invalidateQueries({ queryKey: ['items'] });
      toast.success('Ogłoszenie utworzone!');
      navigate(`/items/${item.id}`);
    },
  });
}

export function useUpdateItem(id: string) {
  const qc = useQueryClient();
  const navigate = useNavigate();

  return useMutation({
    mutationFn: (data: UpdateItemRequest) => itemsApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['items'] });
      toast.success('Ogłoszenie zaktualizowane!');
      navigate(`/items/${id}`);
    },
  });
}

export function useDeleteItem() {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => itemsApi.delete(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['items'] });
      toast.success('Ogłoszenie usunięte.');
    },
  });
}
