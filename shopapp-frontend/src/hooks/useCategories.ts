import { useQuery } from '@tanstack/react-query';
import { categoriesApi } from '@/api/categories';

export function useCategories() {
  return useQuery({
    queryKey: ['categories'],
    queryFn: () => categoriesApi.getAll(),
    staleTime: 10 * 60 * 1000, // 10 minutes — categories change rarely
  });
}
