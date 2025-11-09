import { create } from 'zustand';
import type { UserProfile } from '../types/api.types';
import { authService } from '../services/auth.service';

interface AuthState {
  user: UserProfile | null;
  isLoading: boolean;
  error: string | null;

  // Actions
  login: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  fetchUser: () => Promise<void>;
  clearError: () => void;
  isAdmin: () => boolean;
}

export const useAuthStore = create<AuthState>((set, get) => ({
  user: null,
  isLoading: false,
  error: null,

  login: async (username: string, password: string) => {
    set({ isLoading: true, error: null });
    try {
      await authService.login({ username, password });
      // 登入成功後立即取得使用者資訊
      await get().fetchUser();
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || '登入失敗,請檢查帳號密碼';
      set({ error: errorMessage, isLoading: false });
      throw error;
    }
  },

  logout: async () => {
    set({ isLoading: true });
    try {
      await authService.logout();
    } catch (error) {
      console.error('Logout error:', error);
    } finally {
      set({ user: null, isLoading: false, error: null });
    }
  },

  fetchUser: async () => {
    if (!authService.isAuthenticated()) {
      set({ user: null, isLoading: false });
      return;
    }

    set({ isLoading: true, error: null });
    try {
      const user = await authService.getCurrentUser();
      set({ user, isLoading: false });
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || '無法取得使用者資訊';
      set({ error: errorMessage, isLoading: false, user: null });
    }
  },

  clearError: () => set({ error: null }),

  isAdmin: () => {
    const { user } = get();
    return user?.roles.includes('Admin') ?? false;
  },
}));
