import api from './api';
import type { UsersResponse, StatsResponse } from '../types/api.types';

export const adminService = {
  /**
   * 取得所有使用者清單
   */
  async getAllUsers(): Promise<UsersResponse> {
    const { data } = await api.get<UsersResponse>('/admin/users');
    return data;
  },

  /**
   * 取得系統統計資訊
   */
  async getStats(): Promise<StatsResponse> {
    const { data } = await api.get<StatsResponse>('/admin/stats');
    return data;
  },

  /**
   * 清除快取
   */
  async clearCache(): Promise<{ message: string }> {
    const { data } = await api.post<{ message: string }>('/admin/cache/clear');
    return data;
  },

  /**
   * 撤銷使用者的所有 Token
   */
  async revokeUserTokens(userId: string): Promise<{ message: string }> {
    const { data } = await api.post<{ message: string }>(`/admin/users/${userId}/revoke-tokens`);
    return data;
  },
};
