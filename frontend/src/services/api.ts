import axios, { AxiosError, AxiosInstance, InternalAxiosRequestConfig } from 'axios';
import type { ErrorResponse } from '../types/api.types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'https://localhost:7198/api/v1';

// 建立 Axios 實例
const api: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 10000,
});

// Token 儲存 (記憶體變數,避免 XSS)
let accessToken: string | null = null;

export const setAccessToken = (token: string | null) => {
  accessToken = token;
};

export const getAccessToken = () => accessToken;

// 請求攔截器:自動加入 Bearer Token
api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    if (accessToken && config.headers) {
      config.headers.Authorization = `Bearer ${accessToken}`;
    }
    return config;
  },
  (error) => Promise.reject(error),
);

// 回應攔截器:自動刷新 Token
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ErrorResponse>) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    // 401 錯誤且尚未重試
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        const refreshToken = sessionStorage.getItem('refreshToken');
        if (!refreshToken) {
          // 無 Refresh Token,導向登入
          window.location.href = '/login';
          return Promise.reject(error);
        }

        // 呼叫刷新端點
        const { data } = await axios.post<{ accessToken: string; refreshToken: string }>(
          `${API_BASE_URL}/auth/refresh`,
          { refreshToken },
        );

        // 更新 Token
        setAccessToken(data.accessToken);
        sessionStorage.setItem('refreshToken', data.refreshToken);

        // 重試原請求
        if (originalRequest.headers) {
          originalRequest.headers.Authorization = `Bearer ${data.accessToken}`;
        }
        return api.request(originalRequest);
      } catch (refreshError) {
        // 刷新失敗,清除 Token 並導向登入
        setAccessToken(null);
        sessionStorage.removeItem('refreshToken');
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  },
);

export default api;
