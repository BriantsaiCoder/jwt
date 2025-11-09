import api, { setAccessToken } from './api';
import type { LoginRequest, TokenResponse, UserProfile, RefreshRequest } from '../types/api.types';

export const authService = {
  /**
   * 使用者登入
   */
  async login(credentials: LoginRequest): Promise<TokenResponse> {
    const { data } = await api.post<TokenResponse>('/auth/login', credentials);

    // 儲存 Token
    setAccessToken(data.accessToken);
    sessionStorage.setItem('refreshToken', data.refreshToken);

    return data;
  },

  /**
   * 刷新 Token
   */
  async refresh(refreshToken: string): Promise<TokenResponse> {
    const { data } = await api.post<TokenResponse>('/auth/refresh', { refreshToken });

    // 更新 Token
    setAccessToken(data.accessToken);
    sessionStorage.setItem('refreshToken', data.refreshToken);

    return data;
  },

  /**
   * 使用者登出
   */
  async logout(): Promise<void> {
    const refreshToken = sessionStorage.getItem('refreshToken');
    if (refreshToken) {
      try {
        await api.post('/auth/logout', { refreshToken });
      } catch (error) {
        console.error('Logout error:', error);
      }
    }

    // 清除 Token
    setAccessToken(null);
    sessionStorage.removeItem('refreshToken');
  },

  /**
   * 取得當前使用者資訊
   */
  async getCurrentUser(): Promise<UserProfile> {
    const { data } = await api.get<UserProfile>('/auth/me');
    return data;
  },

  /**
   * 檢查是否已登入
   */
  isAuthenticated(): boolean {
    return !!sessionStorage.getItem('refreshToken');
  },
};
