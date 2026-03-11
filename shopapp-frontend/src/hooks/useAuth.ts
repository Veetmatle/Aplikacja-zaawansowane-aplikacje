import { useMutation, useQueryClient } from '@tanstack/react-query';
import { authApi } from '@/api/auth';
import { cartApi } from '@/api/cart';
import { useAuthStore } from '@/stores/authStore';
import { useCartStore } from '@/stores/cartStore';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import type { LoginRequest, RegisterRequest, ChangePasswordRequest } from '@/types/api';

async function mergeGuestCart() {
  const sessionId = useCartStore.getState().sessionId;
  if (sessionId) {
    try {
      await cartApi.merge(sessionId);
    } catch {
      // merge is best-effort, don't block login
    }
  }
}

export function useLogin() {
  const setAuth = useAuthStore((s) => s.setAuth);
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: LoginRequest) => authApi.login(data),
    onSuccess: async (response) => {
      setAuth(response);
      await mergeGuestCart();
      queryClient.invalidateQueries({ queryKey: ['cart'] });
      toast.success('Zalogowano pomyślnie!');
      const params = new URLSearchParams(window.location.search);
      navigate(params.get('redirect') || '/');
    },
  });
}

export function useRegister() {
  const setAuth = useAuthStore((s) => s.setAuth);
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: RegisterRequest) => authApi.register(data),
    onSuccess: async (response) => {
      setAuth(response);
      await mergeGuestCart();
      queryClient.invalidateQueries({ queryKey: ['cart'] });
      toast.success('Konto utworzone pomyślnie!');
      navigate('/');
    },
  });
}

export function useLogout() {
  const logout = useAuthStore((s) => s.logout);
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => authApi.logout(),
    onSuccess: () => {
      logout();
      queryClient.clear();
      toast.success('Wylogowano.');
      navigate('/');
    },
    onError: () => {
      // Even if API fails, log out locally
      logout();
      queryClient.clear();
      navigate('/');
    },
  });
}

export function useChangePassword() {
  return useMutation({
    mutationFn: (data: ChangePasswordRequest) => authApi.changePassword(data),
    onSuccess: () => {
      toast.success('Hasło zmienione pomyślnie!');
    },
  });
}
