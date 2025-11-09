import api from './api';
import type { UserProfile, UpdateProfileRequest, UpdateProfileResponse, ActivitiesResponse } from '../types/api.types';

export const userService = {
  /**
   * 取得使用者個人資料
   */
  async getProfile(): Promise<UserProfile> {
    const { data } = await api.get<UserProfile>('/user/profile');
    return data;
  },

  /**
   * 更新使用者個人資料
   */
  async updateProfile(request: UpdateProfileRequest): Promise<UpdateProfileResponse> {
    const { data } = await api.put<UpdateProfileResponse>('/user/profile', request);
    return data;
  },

  /**
   * 取得使用者活動記錄
   */
  async getActivities(limit: number = 10): Promise<ActivitiesResponse> {
    const { data } = await api.get<ActivitiesResponse>(`/user/activities?limit=${limit}`);
    return data;
  },
};
